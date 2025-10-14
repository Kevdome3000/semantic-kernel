// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Runtime;
using Microsoft.SemanticKernel.Agents.Runtime.Core;

namespace Microsoft.SemanticKernel.Agents.Magentic;

/// <summary>
/// An <see cref="AgentActor"/> used with the <see cref="MagenticOrchestration{TInput, TOutput}"/>.
/// </summary>
internal sealed class MagenticAgentActor :
    AgentActor,
    IHandle<MagenticMessages.Group>,
    IHandle<MagenticMessages.Reset>,
    IHandle<MagenticMessages.Speak>
{
    private readonly List<ChatMessageContent> _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="MagenticAgentActor"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="runtime">The runtime associated with the agent.</param>
    /// <param name="context">The orchestration context.</param>
    /// <param name="agent">An <see cref="Agent"/>.</param>
    /// <param name="logger">The logger to use for the actor</param>
    public MagenticAgentActor(AgentId id, IAgentRuntime runtime, OrchestrationContext context, Agent agent, ILogger<MagenticAgentActor>? logger = null)
        : base(id, runtime, context, agent, logger)
    {
        _cache = [];
    }

    /// <inheritdoc/>
    public ValueTask HandleAsync(MagenticMessages.Group item, MessageContext messageContext)
    {
        _cache.AddRange(item.Messages);

#if !NETCOREAPP
        return Task.CompletedTask.AsValueTask();
#else
        return ValueTask.CompletedTask;
#endif
    }

    /// <inheritdoc/>
    public async ValueTask HandleAsync(MagenticMessages.Reset item, MessageContext messageContext)
    {
        _cache.Clear();
        await DeleteThreadAsync(messageContext.CancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask HandleAsync(MagenticMessages.Speak item, MessageContext messageContext)
    {
        try
        {
            Logger.LogMagenticAgentInvoke(Id);

            ChatMessageContent response = await InvokeAsync(_cache, messageContext.CancellationToken).ConfigureAwait(false);

            Logger.LogMagenticAgentResult(Id, response.Content);

            _cache.Clear();
            await PublishMessageAsync(response.AsGroupMessage(), Context.Topic).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"ACTOR EXCEPTION: {exception.Message}\n{exception.StackTrace}");
            throw;
        }
    }
}
