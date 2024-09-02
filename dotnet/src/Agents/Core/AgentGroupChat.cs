// Copyright (c) Microsoft. All rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.Agents.Chat;

namespace Microsoft.SemanticKernel.Agents;

/// <summary>
/// A an <see cref="AgentChat"/> that supports multi-turn interactions.
/// </summary>
public sealed class AgentGroupChat : AgentChat
{

    private readonly HashSet<string> _agentIds; // Efficient existence test O(1) vs O(n) for list.

    private readonly List<Agent> _agents; // Maintain order the agents joined the chat

    /// <summary>
    /// Indicates if completion criteria has been met.  If set, no further
    /// agent interactions will occur.  Clear to enable more agent interactions.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Settings for defining chat behavior.
    /// </summary>
    public AgentGroupChatSettings ExecutionSettings { get; set; } = new AgentGroupChatSettings();

    /// <summary>
    /// The agents participating in the chat.
    /// </summary>
    public IReadOnlyList<Agent> Agents => _agents.AsReadOnly();


    /// <summary>
    /// Add a <see cref="Agent"/> to the chat.
    /// </summary>
    /// <param name="agent">The <see cref="KernelAgent"/> to add.</param>
    public void AddAgent(Agent agent)
    {
        if (_agentIds.Add(agent.Id))
        {
            _agents.Add(agent);
        }
    }


    /// <summary>
    /// Process a series of interactions between the <see cref="AgentGroupChat.Agents"/> that have joined this <see cref="AgentGroupChat"/>.
    /// The interactions will proceed according to the <see cref="SelectionStrategy"/> and the <see cref="TerminationStrategy"/>
    /// defined via <see cref="AgentGroupChat.ExecutionSettings"/>.
    /// In the absence of an <see cref="AgentGroupChatSettings.SelectionStrategy"/>, this method will not invoke any agents.
    /// Any agent may be explicitly selected by calling <see cref="AgentGroupChat.InvokeAsync(Agent, CancellationToken)"/>.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>Asynchronous enumeration of messages.</returns>
    public override async IAsyncEnumerable<ChatMessageContent> InvokeAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureStrategyLoggerAssignment();

        if (IsComplete)
        {
            // Throw exception if chat is completed and automatic-reset is not enabled.
            if (!ExecutionSettings.TerminationStrategy.AutomaticReset)
            {
                throw new KernelException("Agent Failure - Chat has completed.");
            }

            IsComplete = false;
        }

        Logger.LogAgentGroupChatInvokingAgents(nameof(InvokeAsync), Agents);

        for (int index = 0; index < ExecutionSettings.TerminationStrategy.MaximumIterations; index++)
        {
            // Identify next agent using strategy
            Agent agent = await this.SelectAgentAsync(cancellationToken).ConfigureAwait(false);

            // Invoke agent and process messages along with termination
            await foreach (var message in InvokeAsync(agent, cancellationToken).ConfigureAwait(false))
            {
                yield return message;
            }

            if (IsComplete)
            {
                break;
            }
        }

        Logger.LogAgentGroupChatYield(nameof(InvokeAsync), IsComplete);
    }


    /// <summary>
    /// Process a single interaction between a given <see cref="Agent"/> an a <see cref="AgentGroupChat"/>.
    /// </summary>
    /// <param name="agent">The agent actively interacting with the chat.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>Asynchronous enumeration of messages.</returns>
    /// <remark>
    /// Specified agent joins the chat.
    /// </remark>>
    public async IAsyncEnumerable<ChatMessageContent> InvokeAsync(
        Agent agent,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureStrategyLoggerAssignment();

        Logger.LogAgentGroupChatInvokingAgent(nameof(InvokeAsync), agent.GetType(), agent.Id);

        AddAgent(agent);

        await foreach (var message in InvokeAgentAsync(agent, cancellationToken).ConfigureAwait(false))
        {
            yield return message;
        }

        IsComplete = await ExecutionSettings.TerminationStrategy.ShouldTerminateAsync(agent, History, cancellationToken).ConfigureAwait(false);

        Logger.LogAgentGroupChatYield(nameof(InvokeAsync), IsComplete);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="AgentGroupChat"/> class.
    /// </summary>
    /// <param name="agents">The agents initially participating in the chat.</param>
    public AgentGroupChat(params Agent[] agents)
    {
        _agents = new(agents);
        _agentIds = new(_agents.Select(a => a.Id));
    }


    private void EnsureStrategyLoggerAssignment()
    {
        // Only invoke logger factory when required.
        if (ExecutionSettings.SelectionStrategy.Logger == NullLogger.Instance)
        {
            ExecutionSettings.SelectionStrategy.Logger = LoggerFactory.CreateLogger(ExecutionSettings.SelectionStrategy.GetType());
        }

        if (ExecutionSettings.TerminationStrategy.Logger == NullLogger.Instance)
        {
            ExecutionSettings.TerminationStrategy.Logger = LoggerFactory.CreateLogger(ExecutionSettings.TerminationStrategy.GetType());
        }
    }


    private async Task<Agent> SelectAgentAsync(CancellationToken cancellationToken)
    {
        this.Logger.LogAgentGroupChatSelectingAgent(nameof(InvokeAsync), this.ExecutionSettings.SelectionStrategy.GetType());

        Agent agent;

        try
        {
            agent = await this.ExecutionSettings.SelectionStrategy.NextAsync(this.Agents, this.History, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            this.Logger.LogAgentGroupChatNoAgentSelected(nameof(InvokeAsync), exception);
            throw;
        }

        this.Logger.LogAgentGroupChatSelectedAgent(nameof(InvokeAsync), agent.GetType(), agent.Id, this.ExecutionSettings.SelectionStrategy.GetType());

        return agent;
    }
}
