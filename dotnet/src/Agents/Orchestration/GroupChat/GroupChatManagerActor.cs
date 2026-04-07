// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents.Runtime;
using Microsoft.SemanticKernel.Agents.Runtime.Core;

namespace Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;

/// <summary>
/// An <see cref="OrchestrationActor"/> used to manage a <see cref="GroupChatOrchestration{TInput, TOutput}"/>.
/// </summary>
internal sealed class GroupChatManagerActor :
    OrchestrationActor,
    IHandle<GroupChatMessages.InputTask>,
    IHandle<GroupChatMessages.Group>
{
    /// <summary>
    /// A common description for the manager.
    /// </summary>
    public const string DefaultDescription = "Orchestrates a team of agents to accomplish a defined task.";

    private readonly AgentType _orchestrationType;
    private readonly GroupChatManager _manager;
    private readonly ChatHistory _chat;
    private readonly GroupChatTeam _team;


    /// <summary>
    /// Initializes a new instance of the <see cref="GroupChatManagerActor"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="runtime">The runtime associated with the agent.</param>
    /// <param name="context">The orchestration context.</param>
    /// <param name="manager">The manages the flow of the group-chat.</param>
    /// <param name="team">The team of agents being orchestrated</param>
    /// <param name="orchestrationType">Identifies the orchestration agent.</param>
    /// <param name="logger">The logger to use for the actor</param>
    public GroupChatManagerActor(
        AgentId id,
        IAgentRuntime runtime,
        OrchestrationContext context,
        GroupChatManager manager,
        GroupChatTeam team,
        AgentType orchestrationType,
        ILogger? logger = null)
        : base(id,
            runtime,
            context,
            DefaultDescription,
            logger)
    {
        _chat = [];
        _manager = manager;
        _orchestrationType = orchestrationType;
        _team = team;
    }


    /// <inheritdoc/>
    public async ValueTask HandleAsync(GroupChatMessages.InputTask item, MessageContext messageContext)
    {
        Logger.LogChatManagerInit(Id);

        _chat.AddRange(item.Messages);

        await PublishMessageAsync(item.Messages.AsGroupMessage(), Context.Topic).ConfigureAwait(false);

        await ManageAsync(messageContext).ConfigureAwait(false);
    }


    /// <inheritdoc/>
    public async ValueTask HandleAsync(GroupChatMessages.Group item, MessageContext messageContext)
    {
        Logger.LogChatManagerInvoke(Id);

        _chat.AddRange(item.Messages);

        await ManageAsync(messageContext).ConfigureAwait(false);
    }


    private async ValueTask ManageAsync(MessageContext messageContext)
    {
        if (_manager.InteractiveCallback != null)
        {
            GroupChatManagerResult<bool> inputResult = await _manager.ShouldRequestUserInput(_chat, messageContext.CancellationToken).ConfigureAwait(false);
            Logger.LogChatManagerInput(Id, inputResult.Value, inputResult.Reason);

            if (inputResult.Value)
            {
                ChatMessageContent input = await _manager.InteractiveCallback.Invoke().ConfigureAwait(false);
                Logger.LogChatManagerUserInput(Id, input.Content);
                _chat.Add(input);
                await PublishMessageAsync(input.AsGroupMessage(), Context.Topic).ConfigureAwait(false);
            }
        }

        GroupChatManagerResult<bool> terminateResult = await _manager.ShouldTerminate(_chat, messageContext.CancellationToken).ConfigureAwait(false);
        Logger.LogChatManagerTerminate(Id, terminateResult.Value, terminateResult.Reason);

        if (terminateResult.Value)
        {
            GroupChatManagerResult<string> filterResult = await _manager.FilterResults(_chat, messageContext.CancellationToken).ConfigureAwait(false);
            Logger.LogChatManagerResult(Id, filterResult.Value, filterResult.Reason);
            await PublishMessageAsync(filterResult.Value.AsResultMessage(), _orchestrationType, messageContext.CancellationToken).ConfigureAwait(false);
            return;
        }

        GroupChatManagerResult<string> selectionResult = await _manager.SelectNextAgent(_chat, _team, messageContext.CancellationToken).ConfigureAwait(false);
        AgentType selectionType = _team[selectionResult.Value].Type;
        Logger.LogChatManagerSelect(Id, selectionType);
        await PublishMessageAsync(new GroupChatMessages.Speak(), selectionType, messageContext.CancellationToken).ConfigureAwait(false);
    }
}
