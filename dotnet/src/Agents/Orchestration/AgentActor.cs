// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents.Runtime;

namespace Microsoft.SemanticKernel.Agents.Orchestration;

/// <summary>
/// An actor that represents an <see cref="Agents.Agent"/>.
/// </summary>
public abstract class AgentActor : OrchestrationActor
{
    private AgentInvokeOptions? _options;
    private ChatMessageContent? _lastResponse;


    /// <summary>
    /// Initializes a new instance of the <see cref="AgentActor"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="runtime">The runtime associated with the agent.</param>
    /// <param name="context">The orchestration context.</param>
    /// <param name="agent">An <see cref="Agents.Agent"/>.</param>
    /// <param name="logger">The logger to use for the actor</param>
    protected AgentActor(
        AgentId id,
        IAgentRuntime runtime,
        OrchestrationContext context,
        Agent agent,
        ILogger? logger = null)
        : base(
            id,
            runtime,
            context,
            VerifyDescription(agent),
            logger)
    {
        Agent = agent;
    }


    /// <summary>
    /// Gets the associated agent.
    /// </summary>
    protected Agent Agent { get; }

    /// <summary>
    /// Gets or sets the current conversation thread used during agent communication.
    /// </summary>
    protected AgentThread? Thread { get; set; }


    /// <summary>
    /// Optionally overridden to create custom invocation options for the agent.
    /// </summary>
    protected virtual AgentInvokeOptions CreateInvokeOptions(Func<ChatMessageContent, Task> messageHandler)
    {
        return new AgentInvokeOptions { OnIntermediateMessage = messageHandler };
    }


    /// <summary>
    /// Optionally overridden to introduce customer filtering logic for the response callback.
    /// </summary>
    /// <param name="response">The agent response</param>
    /// <returns>true if the response should be filtered (hidden)</returns>
    protected virtual bool ResponseCallbackFilter(ChatMessageContent response)
    {
        return false;
    }


    /// <summary>
    /// Deletes the agent thread.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    protected async ValueTask DeleteThreadAsync(CancellationToken cancellationToken)
    {
        if (Thread != null)
        {
            await Thread.DeleteAsync(cancellationToken).ConfigureAwait(false);
            Thread = null;
        }
    }


    /// <summary>
    /// Invokes the agent with a single chat message.
    /// This method sets the message role to <see cref="AuthorRole.User"/> and delegates to the overload accepting multiple messages.
    /// </summary>
    /// <param name="input">The chat message content to send.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that returns the response <see cref="ChatMessageContent"/>.</returns>
    protected ValueTask<ChatMessageContent> InvokeAsync(ChatMessageContent input, CancellationToken cancellationToken)
    {
        return InvokeAsync([input], cancellationToken);
    }


    /// <summary>
    /// Invokes the agent with input messages and respond with both streamed and regular messages.
    /// </summary>
    /// <param name="input">The list of chat messages to send.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that returns the response <see cref="ChatMessageContent"/>.</returns>
    protected async ValueTask<ChatMessageContent> InvokeAsync(IList<ChatMessageContent> input, CancellationToken cancellationToken)
    {
        try
        {
            Context.Cancellation.ThrowIfCancellationRequested();

            _lastResponse = null;

            AgentInvokeOptions options = GetInvokeOptions(HandleMessageAsync);

            if (Context.StreamingResponseCallback == null)
            {
                // No need to utilize streaming if no callback is provided
                await InvokeAsync(input, options, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await InvokeStreamingAsync(input, options, cancellationToken).ConfigureAwait(false);
            }

            return _lastResponse ?? new ChatMessageContent(AuthorRole.Assistant, string.Empty);
        }
        catch (Exception exception)
        {
            Context.FailureCallback.Invoke(exception);
            throw;
        }

        async Task HandleMessageAsync(ChatMessageContent message)
        {
            _lastResponse = message; // Keep track of most recent response for both invocation modes

            if (Context.ResponseCallback is not null && !ResponseCallbackFilter(message))
            {
                await Context.ResponseCallback.Invoke(message).ConfigureAwait(false);
            }
        }
    }


    private async Task InvokeAsync(IList<ChatMessageContent> input, AgentInvokeOptions options, CancellationToken cancellationToken)
    {
        var last = default(AgentResponseItem<ChatMessageContent>)!;
        var hasLast = false;

        await foreach (var item in Agent.InvokeAsync(input,
                Thread,
                options,
                cancellationToken)
            .ConfigureAwait(false))
        {
            hasLast = true;
            last = item;
        }

        if (Thread is null && hasLast)
        {
            Thread = last.Thread;
        }
    }


    private async Task InvokeStreamingAsync(IList<ChatMessageContent> input, AgentInvokeOptions options, CancellationToken cancellationToken)
    {
        IAsyncEnumerable<AgentResponseItem<StreamingChatMessageContent>> streamedResponses =
            Agent.InvokeStreamingAsync(
                input,
                Thread,
                options,
                cancellationToken);

        StreamingChatMessageContent? lastStreamedResponse = null;

        await foreach (AgentResponseItem<StreamingChatMessageContent> streamedResponse in streamedResponses.ConfigureAwait(false))
        {
            Context.Cancellation.ThrowIfCancellationRequested();

            Thread ??= streamedResponse.Thread;

            await HandleStreamedMessage(lastStreamedResponse, false).ConfigureAwait(false);

            lastStreamedResponse = streamedResponse.Message;
        }

        await HandleStreamedMessage(lastStreamedResponse, true).ConfigureAwait(false);

        async ValueTask HandleStreamedMessage(StreamingChatMessageContent? streamedResponse, bool isFinal)
        {
            if (Context.StreamingResponseCallback != null && streamedResponse != null)
            {
                await Context.StreamingResponseCallback.Invoke(streamedResponse, isFinal).ConfigureAwait(false);
            }
        }
    }


    private AgentInvokeOptions GetInvokeOptions(Func<ChatMessageContent, Task> messageHandler)
    {
        return _options ??= CreateInvokeOptions(messageHandler);
    }


    private static string VerifyDescription(Agent agent)
    {
        return agent.Description ?? throw new ArgumentException($"Missing agent description: {agent.Name ?? agent.Id}", nameof(agent));
    }
}
