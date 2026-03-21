// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.Agents.Runtime.InProcess;

/// <summary>
/// Provides an in-process/in-memory implementation of the agent runtime.
/// </summary>
public sealed class InProcessRuntime : IAgentRuntime, IAsyncDisposable
{
    private readonly Dictionary<AgentType, Func<AgentId, IAgentRuntime, ValueTask<IHostableAgent>>> _agentFactories = [];
    private readonly Dictionary<string, ISubscriptionDefinition> _subscriptions = [];
    private readonly ConcurrentQueue<MessageDelivery> _messageDeliveryQueue = new();

    private CancellationTokenSource? _shutdownSource;
    private CancellationTokenSource? _finishSource;
    private Task _messageDeliveryTask = Task.CompletedTask;
    private Func<bool> _shouldContinue = () => true;

    // Exposed for testing purposes.
    internal int messageQueueCount;
    internal readonly Dictionary<AgentId, IHostableAgent> agentInstances = [];

    /// <summary>
    /// Gets or sets a value indicating whether agents should receive messages they send themselves.
    /// </summary>
    public bool DeliverToSelf { get; set; } //= false;


    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await RunUntilIdleAsync().ConfigureAwait(false);
        _shutdownSource?.Dispose();
        _finishSource?.Dispose();
    }


    /// <summary>
    /// Starts the runtime service.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for shutdown requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the runtime is already started.</exception>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_shutdownSource != null)
        {
            throw new InvalidOperationException("Runtime is already running.");
        }

        _shutdownSource = new CancellationTokenSource();
        _messageDeliveryTask = Task.Run(() => RunAsync(_shutdownSource.Token), cancellationToken);

        return Task.CompletedTask;
    }


    /// <summary>
    /// Stops the runtime service.
    /// </summary>
    /// <param name="cancellationToken">Token to propagate when stopping the runtime.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the runtime is in the process of stopping.</exception>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_shutdownSource != null)
        {
            if (_finishSource != null)
            {
                throw new InvalidOperationException("Runtime is already stopping.");
            }

            _finishSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _shutdownSource.Cancel();
        }

        return Task.CompletedTask;
    }


    /// <summary>
    /// This will run until the message queue is empty and then stop the runtime.
    /// </summary>
    public async Task RunUntilIdleAsync()
    {
        Func<bool> oldShouldContinue = _shouldContinue;
        _shouldContinue = () => !_messageDeliveryQueue.IsEmpty;

        // TODO: Do we want detach semantics?
        await _messageDeliveryTask.ConfigureAwait(false);

        _shouldContinue = oldShouldContinue;
    }


    /// <inheritdoc/>
    public ValueTask PublishMessageAsync(
        object message,
        TopicId topic,
        AgentId? sender = null,
        string? messageId = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteTracedAsync(() =>
        {
            MessageDelivery delivery =
                new MessageEnvelope(message, messageId, cancellationToken)
                    .WithSender(sender)
                    .ForPublish(topic, PublishMessageServicerAsync);

            _messageDeliveryQueue.Enqueue(delivery);
            Interlocked.Increment(ref messageQueueCount);

#if !NETCOREAPP
            return Task.CompletedTask.AsValueTask();
#else
            return ValueTask.CompletedTask;
#endif
        });
    }


    /// <inheritdoc/>
    public async ValueTask<object?> SendMessageAsync(
        object message,
        AgentId recipient,
        AgentId? sender = null,
        string? messageId = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteTracedAsync(async () =>
            {
                MessageDelivery delivery =
                    new MessageEnvelope(message, messageId, cancellationToken)
                        .WithSender(sender)
                        .ForSend(recipient, SendMessageServicerAsync);

                _messageDeliveryQueue.Enqueue(delivery);
                Interlocked.Increment(ref messageQueueCount);

                try
                {
                    return await delivery.ResultSink.Future.ConfigureAwait(false);
                }
                catch (TargetInvocationException ex) when (ex.InnerException is OperationCanceledException innerOCEx)
                {
                    throw new OperationCanceledException($"Delivery of message {messageId} was cancelled.", innerOCEx);
                }
            })
            .ConfigureAwait(false);
    }


    /// <inheritdoc/>
    public async ValueTask<AgentId> GetAgentAsync(AgentId agentId, bool lazy = true)
    {
        if (!lazy)
        {
            await EnsureAgentAsync(agentId).ConfigureAwait(false);
        }

        return agentId;
    }


    /// <inheritdoc/>
    public ValueTask<AgentId> GetAgentAsync(AgentType agentType, string key = AgentId.DefaultKey, bool lazy = true)
    {
        return GetAgentAsync(new AgentId(agentType, key), lazy);
    }


    /// <inheritdoc/>
    public ValueTask<AgentId> GetAgentAsync(string agent, string key = AgentId.DefaultKey, bool lazy = true)
    {
        return GetAgentAsync(new AgentId(agent, key), lazy);
    }


    /// <inheritdoc/>
    public async ValueTask<AgentMetadata> GetAgentMetadataAsync(AgentId agentId)
    {
        IHostableAgent agent = await EnsureAgentAsync(agentId).ConfigureAwait(false);
        return agent.Metadata;
    }


    /// <inheritdoc/>
    public async ValueTask<TAgent> TryGetUnderlyingAgentInstanceAsync<TAgent>(AgentId agentId) where TAgent : IHostableAgent
    {
        IHostableAgent agent = await EnsureAgentAsync(agentId).ConfigureAwait(false);

        if (agent is not TAgent concreteAgent)
        {
            throw new InvalidOperationException($"Agent with name {agentId.Type} is not of type {typeof(TAgent).Name}.");
        }

        return concreteAgent;
    }


    /// <inheritdoc/>
    public async ValueTask LoadAgentStateAsync(AgentId agentId, JsonElement state)
    {
        IHostableAgent agent = await EnsureAgentAsync(agentId).ConfigureAwait(false);
        await agent.LoadStateAsync(state).ConfigureAwait(false);
    }


    /// <inheritdoc/>
    public async ValueTask<JsonElement> SaveAgentStateAsync(AgentId agentId)
    {
        IHostableAgent agent = await EnsureAgentAsync(agentId).ConfigureAwait(false);
        return await agent.SaveStateAsync().ConfigureAwait(false);
    }


    /// <inheritdoc/>
    public ValueTask AddSubscriptionAsync(ISubscriptionDefinition subscription)
    {
        if (_subscriptions.ContainsKey(subscription.Id))
        {
            throw new InvalidOperationException($"Subscription with id {subscription.Id} already exists.");
        }

        _subscriptions.Add(subscription.Id, subscription);

#if !NETCOREAPP
        return Task.CompletedTask.AsValueTask();
#else
        return ValueTask.CompletedTask;
#endif
    }


    /// <inheritdoc/>
    public ValueTask RemoveSubscriptionAsync(string subscriptionId)
    {
        if (!_subscriptions.ContainsKey(subscriptionId))
        {
            throw new InvalidOperationException($"Subscription with id {subscriptionId} does not exist.");
        }

        _subscriptions.Remove(subscriptionId);

#if !NETCOREAPP
        return Task.CompletedTask.AsValueTask();
#else
        return ValueTask.CompletedTask;
#endif
    }


    /// <inheritdoc/>
    public async ValueTask LoadStateAsync(JsonElement state)
    {
        foreach (JsonProperty agentIdStr in state.EnumerateObject())
        {
            AgentId agentId = AgentId.FromStr(agentIdStr.Name);

            if (_agentFactories.ContainsKey(agentId.Type))
            {
                IHostableAgent agent = await EnsureAgentAsync(agentId).ConfigureAwait(false);
                await agent.LoadStateAsync(agentIdStr.Value).ConfigureAwait(false);
            }
        }
    }


    /// <inheritdoc/>
    public async ValueTask<JsonElement> SaveStateAsync()
    {
        Dictionary<string, JsonElement> state = [];

        foreach (AgentId agentId in agentInstances.Keys)
        {
            JsonElement agentState = await agentInstances[agentId].SaveStateAsync().ConfigureAwait(false);
            state[agentId.ToString()] = agentState;
        }
        return JsonSerializer.SerializeToElement(state);
    }


    /// <summary>
    /// Registers an agent factory with the runtime, associating it with a specific agent type.
    /// </summary>
    /// <typeparam name="TAgent">The type of agent created by the factory.</typeparam>
    /// <param name="type">The agent type to associate with the factory.</param>
    /// <param name="factoryFunc">A function that asynchronously creates the agent instance.</param>
    /// <returns>A task representing the asynchronous operation, returning the registered agent type.</returns>
    public ValueTask<AgentType> RegisterAgentFactoryAsync<TAgent>(AgentType type, Func<AgentId, IAgentRuntime, ValueTask<TAgent>> factoryFunc) where TAgent : IHostableAgent // Declare the lambda return type explicitly, as otherwise the compiler will infer 'ValueTask<TAgent>'
    // and recurse into the same call, causing a stack overflow.
    {
    return RegisterAgentFactoryAsync(type, async ValueTask<IHostableAgent> (agentId, runtime) => await factoryFunc(agentId, runtime).ConfigureAwait(false));
    }


    /// <inheritdoc/>
    public ValueTask<AgentType> RegisterAgentFactoryAsync(AgentType type, Func<AgentId, IAgentRuntime, ValueTask<IHostableAgent>> factoryFunc)
    {
        if (_agentFactories.ContainsKey(type))
        {
            throw new InvalidOperationException($"Agent with type {type} already exists.");
        }

        _agentFactories.Add(type, factoryFunc);

#if !NETCOREAPP
        return type.AsValueTask();
#else
        return ValueTask.FromResult(type);
#endif
    }


    /// <inheritdoc/>
    public ValueTask<AgentProxy> TryGetAgentProxyAsync(AgentId agentId)
    {
        AgentProxy proxy = new(agentId, this);

#if !NETCOREAPP
        return proxy.AsValueTask();
#else
        return ValueTask.FromResult(proxy);
#endif
    }


    private ValueTask ProcessNextMessageAsync(CancellationToken cancellation = default)
    {
        if (_messageDeliveryQueue.TryDequeue(out MessageDelivery? delivery))
        {
            Interlocked.Decrement(ref messageQueueCount);
            Debug.WriteLine($"Processing message {delivery.Message.MessageId}...");
            return delivery.InvokeAsync(cancellation);
        }

#if !NETCOREAPP
        return Task.CompletedTask.AsValueTask();
#else
        return ValueTask.CompletedTask;
#endif
    }


    private async Task RunAsync(CancellationToken cancellation)
    {
        Dictionary<Guid, Task> pendingTasks = [];

        while (!cancellation.IsCancellationRequested && _shouldContinue())
        {
            // Get a unique task id
            Guid taskId;

            do
            {
                taskId = Guid.NewGuid();
            } while (pendingTasks.ContainsKey(taskId));

            // There is potentially a race condition here, but even if we leak a Task, we will
            // still catch it on the Finish() pass.
            ValueTask processTask = ProcessNextMessageAsync(cancellation);
            await Task.Yield();

            // Check if the task is already completed
            if (processTask.IsCompleted)
            {
                continue;
            }

            Task actualTask = processTask.AsTask();
            pendingTasks.Add(taskId, actualTask.ContinueWith(t => pendingTasks.Remove(taskId), TaskScheduler.Current));
        }

        // The pending task dictionary may contain null values when a race condition is experienced during
        // the prior "ContinueWith" call.  This could be solved with a ConcurrentDictionary, but locking
        // is entirely undesirable in this context.
        await Task.WhenAll([.. pendingTasks.Values.Where(task => task is not null)]).ConfigureAwait(false);
        await FinishAsync(_finishSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
    }


    private async ValueTask PublishMessageServicerAsync(MessageEnvelope envelope, CancellationToken deliveryToken)
    {
        if (!envelope.Topic.HasValue)
        {
            throw new InvalidOperationException("Message must have a topic to be published.");
        }

        List<Task>? tasks = null;
        TopicId topic = envelope.Topic.Value;

        foreach (ISubscriptionDefinition subscription in _subscriptions.Values.Where(subscription => subscription.Matches(topic)))
        {
            (tasks ??= []).Add(ProcessSubscriptionAsync(envelope,
                topic,
                subscription,
                deliveryToken));
        }

        if (tasks is not null)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        async Task ProcessSubscriptionAsync(
            MessageEnvelope envelope,
            TopicId topic,
            ISubscriptionDefinition subscription,
            CancellationToken deliveryToken)
        {
            deliveryToken.ThrowIfCancellationRequested();

            AgentId? sender = envelope.Sender;

            using CancellationTokenSource combinedSource = CancellationTokenSource.CreateLinkedTokenSource(envelope.Cancellation, deliveryToken);
            MessageContext messageContext = new(envelope.MessageId, combinedSource.Token)
            {
                Sender = sender,
                Topic = topic,
                IsRpc = false
            };

            AgentId agentId = subscription.MapToAgent(topic);

            if (!DeliverToSelf && sender.HasValue && sender == agentId)
            {
                return;
            }

            IHostableAgent agent = await EnsureAgentAsync(agentId).ConfigureAwait(false);

            await agent.OnMessageAsync(envelope.Message, messageContext).ConfigureAwait(false);
        }
    }


    private async ValueTask<object?> SendMessageServicerAsync(MessageEnvelope envelope, CancellationToken deliveryToken)
    {
        if (!envelope.Receiver.HasValue)
        {
            throw new InvalidOperationException("Message must have a receiver to be sent.");
        }

        using CancellationTokenSource combinedSource = CancellationTokenSource.CreateLinkedTokenSource(envelope.Cancellation, deliveryToken);
        MessageContext messageContext = new(envelope.MessageId, combinedSource.Token)
        {
            Sender = envelope.Sender,
            IsRpc = false
        };

        AgentId receiver = envelope.Receiver.Value;
        IHostableAgent agent = await EnsureAgentAsync(receiver).ConfigureAwait(false);

        return await agent.OnMessageAsync(envelope.Message, messageContext).ConfigureAwait(false);
    }


    private async ValueTask<IHostableAgent> EnsureAgentAsync(AgentId agentId)
    {
        if (!agentInstances.TryGetValue(agentId, out IHostableAgent? agent))
        {
            if (!_agentFactories.TryGetValue(agentId.Type, out Func<AgentId, IAgentRuntime, ValueTask<IHostableAgent>>? factoryFunc))
            {
                throw new InvalidOperationException($"Agent with name {agentId.Type} not found.");
            }

            agent = await factoryFunc(agentId, this).ConfigureAwait(false);
            agentInstances.Add(agentId, agent);
        }

        return agentInstances[agentId];
    }


    private async Task FinishAsync(CancellationToken token)
    {
        foreach (IHostableAgent agent in agentInstances.Values)
        {
            if (!token.IsCancellationRequested)
            {
                await agent.CloseAsync().ConfigureAwait(false);
            }
        }

        _shutdownSource?.Dispose();
        _finishSource?.Dispose();
        _finishSource = null;
        _shutdownSource = null;
    }


#pragma warning disable CA1822 // Mark members as static
    private ValueTask<T> ExecuteTracedAsync<T>(Func<ValueTask<T>> func)
#pragma warning restore CA1822 // Mark members as static
    {
        // TODO: Bind tracing
        return func();
    }


#pragma warning disable CA1822 // Mark members as static
    private ValueTask ExecuteTracedAsync(Func<ValueTask> func)
#pragma warning restore CA1822 // Mark members as static
    {
        // TODO: Bind tracing
        return func();
    }
}
