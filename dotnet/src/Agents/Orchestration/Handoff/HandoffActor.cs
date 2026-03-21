// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents.Runtime;
using Microsoft.SemanticKernel.Agents.Runtime.Core;

namespace Microsoft.SemanticKernel.Agents.Orchestration.Handoff;

/// <summary>
/// An actor used with the <see cref="HandoffOrchestration{TInput,TOutput}"/>.
/// </summary>
internal sealed class HandoffActor :
    AgentActor,
    IHandle<HandoffMessages.InputTask>,
    IHandle<HandoffMessages.Request>,
    IHandle<HandoffMessages.Response>
{
    private readonly HandoffLookup _handoffs;
    private readonly AgentType _resultHandoff;
    private readonly List<ChatMessageContent> _cache;

    private string? _handoffAgent;
    private string? _taskSummary;


    /// <summary>
    /// Initializes a new instance of the <see cref="HandoffActor"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="runtime">The runtime associated with the agent.</param>
    /// <param name="context">The orchestration context.</param>
    /// <param name="agent">An <see cref="Agent"/>.</param>
    /// <param name="handoffs">The handoffs available to this agent</param>
    /// <param name="resultHandoff">The handoff agent for capturing the result.</param>
    /// <param name="logger">The logger to use for the actor</param>
    public HandoffActor(
        AgentId id,
        IAgentRuntime runtime,
        OrchestrationContext context,
        Agent agent,
        HandoffLookup handoffs,
        AgentType resultHandoff,
        ILogger<HandoffActor>? logger = null)
        : base(id,
            runtime,
            context,
            agent,
            logger)
    {
        if (handoffs.ContainsKey(agent.Name ?? agent.Id))
        {
            throw new ArgumentException($"The agent {agent.Name ?? agent.Id} cannot have a handoff to itself.", nameof(handoffs));
        }

        _cache = [];
        _handoffs = handoffs;
        _resultHandoff = resultHandoff;
    }


    /// <summary>
    /// Gets or sets the callback to be invoked for interactive input.
    /// </summary>
    public OrchestrationInteractiveCallback? InteractiveCallback { get; init; }


    /// <inheritdoc/>
    protected override bool ResponseCallbackFilter(ChatMessageContent response)
    {
        return response.Role == AuthorRole.Tool;
    }


    /// <inheritdoc/>
    protected override AgentInvokeOptions CreateInvokeOptions(Func<ChatMessageContent, Task> messageHandler)
    {
        // Clone kernel to avoid modifying the original
        Kernel kernel = Agent.Kernel.Clone();
        kernel.AutoFunctionInvocationFilters.Add(new HandoffInvocationFilter());
        kernel.Plugins.Add(CreateHandoffPlugin());

        // Create invocation options that use auto-function invocation and our modified kernel.
        AgentInvokeOptions options =
            new()
            {
                Kernel = kernel,
                KernelArguments = new(new PromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new()
                    {
                        RetainArgumentTypes = true
                    })
                }),
                OnIntermediateMessage = messageHandler
            };

        return options;
    }


    /// <inheritdoc/>
    public ValueTask HandleAsync(HandoffMessages.InputTask item, MessageContext messageContext)
    {
        _taskSummary = null;
        _cache.AddRange(item.Messages);

#if !NETCOREAPP
        return Task.CompletedTask.AsValueTask();
#else
        return ValueTask.CompletedTask;
#endif
    }


    /// <inheritdoc/>
    public ValueTask HandleAsync(HandoffMessages.Response item, MessageContext messageContext)
    {
        _cache.Add(item.Message);

#if !NETCOREAPP
        return Task.CompletedTask.AsValueTask();
#else
        return ValueTask.CompletedTask;
#endif
    }


    /// <inheritdoc/>
    public async ValueTask HandleAsync(HandoffMessages.Request item, MessageContext messageContext)
    {
        Logger.LogHandoffAgentInvoke(Id);

        while (_taskSummary == null)
        {
            ChatMessageContent response = await InvokeAsync(_cache, messageContext.CancellationToken).ConfigureAwait(false);
            _cache.Clear();

            Logger.LogHandoffAgentResult(Id, response.Content);

            // The response can potentially be a TOOL message from the Handoff plugin due to the filter
            // which will terminate the conversation when a function from the handoff plugin is called.
            // Since we don't want to publish that message, so we only publish if the response is an ASSISTANT message.
            if (response.Role == AuthorRole.Assistant)
            {
                await PublishMessageAsync(new HandoffMessages.Response { Message = response },
                        Context.Topic,
                        null,
                        messageContext.CancellationToken)
                    .ConfigureAwait(false);
            }

            if (_handoffAgent != null)
            {
                AgentType handoffType = _handoffs[_handoffAgent].AgentType;
                await PublishMessageAsync(new HandoffMessages.Request(), handoffType, messageContext.CancellationToken).ConfigureAwait(false);

                _handoffAgent = null;
                break;
            }

            if (InteractiveCallback != null && _taskSummary == null)
            {
                ChatMessageContent input = await InteractiveCallback().ConfigureAwait(false);
                await PublishMessageAsync(new HandoffMessages.Response { Message = input },
                        Context.Topic,
                        null,
                        messageContext.CancellationToken)
                    .ConfigureAwait(false);
                _cache.Add(input);
                continue;
            }

            await EndAsync(response.Content ?? "No handoff or human response function requested. Ending task.", messageContext.CancellationToken).ConfigureAwait(false);
        }
    }


    private KernelPlugin CreateHandoffPlugin()
    {
        return KernelPluginFactory.CreateFromFunctions(HandoffInvocationFilter.HandoffPlugin, CreateHandoffFunctions());

        IEnumerable<KernelFunction> CreateHandoffFunctions()
        {
            yield return KernelFunctionFactory.CreateFromMethod(
                EndAsync,
                functionName: "end_task_with_summary",
                description: "Complete the task with a summary when no further requests are given.");

            foreach (KeyValuePair<string, (AgentType _, string Description)> handoff in _handoffs)
            {
                KernelFunction kernelFunction =
                    KernelFunctionFactory.CreateFromMethod(
                        (CancellationToken cancellationToken) => HandoffAsync(handoff.Key, cancellationToken),
                        functionName: $"transfer_to_{handoff.Key}",
                        description: handoff.Value.Description);

                yield return kernelFunction;
            }
        }
    }


    private ValueTask HandoffAsync(string agentName, CancellationToken cancellationToken = default)
    {
        Logger.LogHandoffFunctionCall(Id, agentName);
        _handoffAgent = agentName;

#if !NETCOREAPP
        return Task.CompletedTask.AsValueTask();
#else
        return ValueTask.CompletedTask;
#endif
    }


    private async ValueTask EndAsync(string summary, CancellationToken cancellationToken)
    {
        Logger.LogHandoffSummary(Id, summary);
        _taskSummary = summary;
        await PublishMessageAsync(new HandoffMessages.Result { Message = new ChatMessageContent(AuthorRole.Assistant, summary) }, _resultHandoff, cancellationToken).ConfigureAwait(false);
    }
}
