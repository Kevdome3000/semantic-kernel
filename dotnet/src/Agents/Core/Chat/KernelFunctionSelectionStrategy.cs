// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Agents.Internal;

namespace Microsoft.SemanticKernel.Agents.Chat;

/// <summary>
/// Determines agent selection based on the evaluation of a <see cref="KernelFunction"/>.
/// </summary>
/// <param name="function">A <see cref="KernelFunction"/> used for selection criteria.</param>
/// <param name="kernel">A kernel instance with services for function execution.</param>
[Experimental("SKEXP0110")]
public class KernelFunctionSelectionStrategy(KernelFunction function, Kernel kernel) : SelectionStrategy
{

    /// <summary>
    /// The default value for <see cref="KernelFunctionSelectionStrategy.AgentsVariableName"/>.
    /// </summary>
    public const string DefaultAgentsVariableName = "_agents_";

    /// <summary>
    /// The default value for <see cref="KernelFunctionSelectionStrategy.HistoryVariableName"/>.
    /// </summary>
    public const string DefaultHistoryVariableName = "_history_";

    /// <summary>
    /// Gets the <see cref="KernelArguments"/> key associated with the list of agent names when
    /// invoking <see cref="KernelFunctionSelectionStrategy.Function"/>.
    /// </summary>
    public string AgentsVariableName { get; init; } = DefaultAgentsVariableName;

    /// <summary>
    /// Gets the <see cref="KernelArguments"/> key associated with the chat history when
    /// invoking <see cref="KernelFunctionSelectionStrategy.Function"/>.
    /// </summary>
    public string HistoryVariableName { get; init; } = DefaultHistoryVariableName;

    /// <summary>
    /// Gets the optional arguments used when invoking <see cref="KernelFunctionSelectionStrategy.Function"/>.
    /// </summary>
    public KernelArguments? Arguments { get; init; }

    /// <summary>
    /// Gets the <see cref="Microsoft.SemanticKernel.Kernel"/> used when invoking <see cref="KernelFunctionSelectionStrategy.Function"/>.
    /// </summary>
    public Kernel Kernel => kernel;

    /// <summary>
    /// Gets the <see cref="KernelFunction"/> invoked as selection criteria.
    /// </summary>
    public KernelFunction Function { get; } = function;

    /// <summary>
    /// Gets a value that indicates whether only the agent name is included in the history when invoking <see cref="KernelFunctionTerminationStrategy.Function"/>.
    /// </summary>
    public bool EvaluateNameOnly { get; init; }

    /// <summary>
    /// Gets an optional <see cref="IChatHistoryReducer"/> to reduce the history.
    /// </summary>
    public IChatHistoryReducer? HistoryReducer { get; init; }

    /// <summary>
    /// Gets a value that indicates whether <see cref="SelectionStrategy.InitialAgent"/> is used in the event of a failure to select an agent.
    /// </summary>
    public bool UseInitialAgentAsFallback { get; init; }

    /// <summary>
    /// Gets a callback responsible for translating the <see cref="FunctionResult"/>
    /// to the termination criteria.
    /// </summary>
    public Func<FunctionResult, string> ResultParser { get; init; } = result => result.GetValue<string>() ?? string.Empty;


    /// <inheritdoc/>
    protected sealed override async Task<Agent> SelectAgentAsync(IReadOnlyList<Agent> agents, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
    {
        history = await history.ReduceAsync(HistoryReducer, cancellationToken).ConfigureAwait(false);

        KernelArguments originalArguments = Arguments ?? [];

        KernelArguments arguments =
            new(originalArguments, originalArguments.ExecutionSettings?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            {
                { AgentsVariableName, string.Join(",", agents.Select(a => a.Name)) },
                { HistoryVariableName, ChatMessageForPrompt.Format(history, EvaluateNameOnly) }
            };

        Logger.LogKernelFunctionSelectionStrategyInvokingFunction(nameof(NextAsync), Function.PluginName, Function.Name);

        FunctionResult result = await Function.InvokeAsync(Kernel, arguments, cancellationToken).ConfigureAwait(false);

        Logger.LogKernelFunctionSelectionStrategyInvokedFunction(nameof(NextAsync),
            Function.PluginName,
            Function.Name,
            result.ValueType);

        string? agentName = ResultParser.Invoke(result);

        if (string.IsNullOrEmpty(agentName) && (!UseInitialAgentAsFallback || InitialAgent == null))
        {
            throw new KernelException("Agent Failure - Strategy unable to determine next agent.");
        }

        Agent? agent = agents.FirstOrDefault(a => (a.Name ?? a.Id) == agentName);

        if (agent == null && UseInitialAgentAsFallback)
        {
            agent = InitialAgent;
        }

        return agent ?? throw new KernelException($"Agent Failure - Strategy unable to select next agent: {agentName}");
    }

}
