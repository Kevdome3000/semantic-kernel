// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents.Runtime;
using Microsoft.SemanticKernel.Agents.Runtime.Core;

namespace Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;

/// <summary>
/// An <see cref="AgentActor"/> used with the <see cref="GroupChatOrchestration{TInput, TOutput}"/>.
/// </summary>
internal sealed class GroupChatAgentActor :
    AgentActor,
    IHandle<GroupChatMessages.Group>,
    IHandle<GroupChatMessages.Reset>,
    IHandle<GroupChatMessages.Speak>
{
    private readonly List<ChatMessageContent> _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupChatAgentActor"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="runtime">The runtime associated with the agent.</param>
    /// <param name="context">The orchestration context.</param>
    /// <param name="agent">An <see cref="Agent"/>.</param>
    /// <param name="logger">The logger to use for the actor</param>
    public GroupChatAgentActor(
        AgentId id,
        IAgentRuntime runtime,
        OrchestrationContext context,
        Agent agent,
        ILogger<GroupChatAgentActor>? logger = null)
        : base(id,
            runtime,
            context,
            agent,
            logger)
    {
        _cache = [];
    }

    /// <inheritdoc/>
    public ValueTask HandleAsync(GroupChatMessages.Group item, MessageContext messageContext)
    {
        _cache.AddRange(item.Messages);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask HandleAsync(GroupChatMessages.Reset item, MessageContext messageContext)
    {
        await DeleteThreadAsync(messageContext.CancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask HandleAsync(GroupChatMessages.Speak item, MessageContext messageContext)
    {
        Logger.LogChatAgentInvoke(Id);

        ChatMessageContent response = await InvokeAsync(_cache, messageContext.CancellationToken).ConfigureAwait(false);

        Logger.LogChatAgentResult(Id, response.Content);

        _cache.Clear();
        await PublishMessageAsync(response.AsGroupMessage(), Context.Topic).ConfigureAwait(false);
    }
}
