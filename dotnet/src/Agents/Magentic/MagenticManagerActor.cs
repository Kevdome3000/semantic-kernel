// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Runtime;
using Microsoft.SemanticKernel.Agents.Runtime.Core;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Microsoft.SemanticKernel.Agents.Magentic;

/// <summary>
/// An <see cref="OrchestrationActor"/> used to manage a <see cref="MagenticOrchestration{TInput, TOutput}"/>.
/// </summary>
internal sealed class MagenticManagerActor :
    OrchestrationActor,
    IHandle<MagenticMessages.InputTask>,
    IHandle<MagenticMessages.Group>
{
    /// <summary>
    /// A common description for the manager.
    /// </summary>
    public const string DefaultDescription = "Orchestrates a team of agents to accomplish a defined task.";

    private readonly AgentType _orchestrationType;
    private readonly MagenticManager _manager;
    private readonly ChatHistory _chat;
    private readonly MagenticTeam _team;

    private IReadOnlyList<ChatMessageContent> _inputTask = [];
    private int _invocationCount;
    private int _stallCount = 0;
    private int _retryCount = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="MagenticManagerActor"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="runtime">The runtime associated with the agent.</param>
    /// <param name="context">The orchestration context.</param>
    /// <param name="manager">The manages the flow of the group-chat.</param>
    /// <param name="team">The team of agents being orchestrated</param>
    /// <param name="orchestrationType">Identifies the orchestration agent.</param>
    /// <param name="logger">The logger to use for the actor</param>
    public MagenticManagerActor(AgentId id, IAgentRuntime runtime, OrchestrationContext context, MagenticManager manager, MagenticTeam team, AgentType orchestrationType, ILogger? logger = null)
        : base(id, runtime, context, DefaultDescription, logger)
    {
        _chat = [];
        _manager = manager;
        _orchestrationType = orchestrationType;
        _team = team;

        Debug.WriteLine($"TEAM:\n{team.FormatList()}");
    }

    /// <inheritdoc/>
    public async ValueTask HandleAsync(MagenticMessages.InputTask item, MessageContext messageContext)
    {
        Logger.LogMagenticManagerInit(Id);

        _chat.AddRange(item.Messages);
        _inputTask = item.Messages.ToList().AsReadOnly();

        await PublishMessageAsync(item.Messages.AsGroupMessage(), Context.Topic).ConfigureAwait(false);
        await PrepareAsync(isReset: false, messageContext.CancellationToken).ConfigureAwait(false);
        await ManageAsync(messageContext.CancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask HandleAsync(MagenticMessages.Group item, MessageContext messageContext)
    {
        Logger.LogMagenticManagerInvoke(Id);

        _chat.AddRange(item.Messages);

        await ManageAsync(messageContext.CancellationToken).ConfigureAwait(false);
    }

    private async ValueTask ManageAsync(CancellationToken cancellationToken)
    {
        bool isStalled = false;
        string? stallMessage = null;

        do
        {
            string agentName = string.Empty;
            string agentInstruction = string.Empty;
            try
            {
                MagenticManagerContext context = CreateContext();
                MagenticProgressLedger status = await _manager.EvaluateTaskProgressAsync(context, cancellationToken).ConfigureAwait(false);

                Debug.WriteLine($"STATUS:\n{status.ToJson()}");

                if (status.IsTaskComplete)
                {
                    ChatMessageContent finalAnswer = await _manager.PrepareFinalAnswerAsync(context, cancellationToken).ConfigureAwait(false);
                    await PublishMessageAsync(finalAnswer.AsResultMessage(), _orchestrationType, cancellationToken).ConfigureAwait(false);
                    break;
                }

                isStalled = !status.IsTaskProgressing || status.IsTaskInLoop;
                agentName = status.Name;
                agentInstruction = status.Instruction;
            }
            catch (Exception exception) when (!exception.IsCriticalException())
            {
                Logger.LogMagenticManagerStatusFailure(Context.Topic, exception);
                isStalled = true;
                stallMessage = exception.Message;
            }

            bool hasAgent = _team.TryGetValue(agentName, out (string Type, string Description) agent);
            if (!hasAgent)
            {
                isStalled = true;
                stallMessage = $"Invalid agent selected: {agentName}";
            }

            if (isStalled)
            {
                ++_stallCount;

                Debug.WriteLine($"TASK STALLED: #{_stallCount}/{_manager.MaximumStallCount} [#{_retryCount}] -  {stallMessage}");
            }
            else
            {
                _stallCount = Math.Max(0, _stallCount - 1);
            }

            bool needsReset = _stallCount >= _manager.MaximumStallCount;

            if (!needsReset && hasAgent)
            {
                ++_invocationCount;

                if (_invocationCount >= _manager.MaximumInvocationCount)
                {
                    Logger.LogMagenticManagerTaskFailed(Context.Topic);
                    try
                    {
                        var partialResult = _chat.Last((message) => message.Role == AuthorRole.Assistant);
                        await PublishMessageAsync(partialResult.AsResultMessage(), _orchestrationType, cancellationToken).ConfigureAwait(false);
                    }
                    catch (InvalidOperationException)
                    {
                        await PublishMessageAsync("I've reaches the maximum number of invocations. No partial result available.".AsResultMessage(), _orchestrationType, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                }

                ChatMessageContent instruction = new(AuthorRole.Assistant, agentInstruction);
                _chat.Add(instruction);
                await PublishMessageAsync(instruction.AsGroupMessage(), Context.Topic, messageId: null, cancellationToken).ConfigureAwait(false);
                await PublishMessageAsync(new MagenticMessages.Speak(), agent.Type, cancellationToken).ConfigureAwait(false);
                break;
            }

            if (_stallCount >= _manager.MaximumStallCount)
            {
                if (_retryCount >= _manager.MaximumResetCount)
                {
                    Logger.LogMagenticManagerTaskFailed(Context.Topic);
                    try
                    {
                        var partialResult = _chat.Last((message) => message.Role == AuthorRole.Assistant);
                        await PublishMessageAsync(partialResult.AsResultMessage(), _orchestrationType, cancellationToken).ConfigureAwait(false);
                    }
                    catch (InvalidOperationException)
                    {
                        await PublishMessageAsync("I've experienced multiple failures and am unable to continue. No partial result available.".AsResultMessage(), _orchestrationType, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                }

                _retryCount++;
                _stallCount = 0;

                Logger.LogMagenticManagerTaskReset(Context.Topic, _retryCount);
                Debug.WriteLine($"TASK RESET [#{_retryCount}]");

                await PublishMessageAsync(new MagenticMessages.Reset(), Context.Topic, messageId: null, cancellationToken).ConfigureAwait(false);
                await PrepareAsync(isReset: true, cancellationToken).ConfigureAwait(false);
            }
        }
        while (isStalled);
    }

    private async ValueTask PrepareAsync(bool isReset, CancellationToken cancellationToken)
    {
        ChatHistory internalChat = [.. _chat];
        _chat.Clear();

        MagenticManagerContext context = CreateContext(internalChat);

        IList<ChatMessageContent> plan;
        if (isReset)
        {
            plan = await _manager.PlanAsync(context, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            plan = await _manager.ReplanAsync(context, cancellationToken).ConfigureAwait(false);
        }

        _chat.AddRange(plan);
    }

    private MagenticManagerContext CreateContext(ChatHistory? chat = null) =>
        new(_team, _inputTask, (chat ?? _chat), _invocationCount, _stallCount, _retryCount);
}
