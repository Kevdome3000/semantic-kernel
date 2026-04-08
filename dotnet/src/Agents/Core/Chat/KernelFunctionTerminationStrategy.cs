// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.Agents.Internal;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Microsoft.SemanticKernel.Agents.Chat;

/// <summary>
/// Signals termination based on the evaluation of a <see cref="KernelFunction"/>.
/// </summary>
/// <param name="function">A <see cref="KernelFunction"/> used for termination criteria.</param>
/// <param name="kernel">A kernel instance with services for function execution.</param>
[Experimental("SKEXP0110")]
public class KernelFunctionTerminationStrategy(KernelFunction function, Kernel kernel) : TerminationStrategy
{
    /// <summary>
    /// The default value for <see cref="KernelFunctionTerminationStrategy.AgentVariableName"/>.
    /// </summary>
    public const string DefaultAgentVariableName = "_agent_";

    /// <summary>
    /// The default value for <see cref="KernelFunctionTerminationStrategy.HistoryVariableName"/>.
    /// </summary>
    public const string DefaultHistoryVariableName = "_history_";

    /// <summary>
    /// Gets the <see cref="KernelArguments"/> key associated with the agent name when
    /// invoking <see cref="KernelFunctionSelectionStrategy.Function"/>.
    /// </summary>
    public string AgentVariableName { get; init; } = DefaultAgentVariableName;

    /// <summary>
    /// Gets the <see cref="KernelArguments"/> key associated with the chat history when
    /// invoking <see cref="KernelFunctionTerminationStrategy.Function"/>.
    /// </summary>
    public string HistoryVariableName { get; init; } = DefaultHistoryVariableName;

    /// <summary>
    /// Gets optional arguments used when invoking <see cref="KernelFunctionTerminationStrategy.Function"/>.
    /// </summary>
    public KernelArguments? Arguments { get; init; }

    /// <summary>
    /// Gets the <see cref="Microsoft.SemanticKernel.Kernel"/> used when invoking <see cref="KernelFunctionTerminationStrategy.Function"/>.
    /// </summary>
    public Kernel Kernel => kernel;

    /// <summary>
    /// Gets the <see cref="KernelFunction"/> invoked as termination criteria.
    /// </summary>
    public KernelFunction Function { get; } = function;

    /// <summary>
    /// Gets a value that indicates whether only the agent name is included in the history when invoking <see cref="KernelFunctionTerminationStrategy.Function"/>.
    /// </summary>
    public bool EvaluateNameOnly { get; init; }

    /// <summary>
    /// Gets a callback responsible for translating the <see cref="FunctionResult"/>
    /// to the termination criteria.
    /// </summary>
    public Func<FunctionResult, bool> ResultParser { get; init; } = _ => true;

    /// <summary>
    /// Gets an optional <see cref="IChatHistoryReducer"/> to reduce the history.
    /// </summary>
    public IChatHistoryReducer? HistoryReducer { get; init; }


    /// <inheritdoc/>
    protected sealed override async Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
    {
        history = await history.ReduceAsync(HistoryReducer, cancellationToken).ConfigureAwait(false);

        KernelArguments originalArguments = Arguments ?? [];
        KernelArguments arguments =
            new(originalArguments, originalArguments.ExecutionSettings?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            {
                { AgentVariableName, agent.Name ?? agent.Id },
                { HistoryVariableName, ChatMessageForPrompt.Format(history, EvaluateNameOnly) }
            };

        Logger.LogKernelFunctionTerminationStrategyInvokingFunction(nameof(ShouldAgentTerminateAsync), Function.PluginName, Function.Name);

        FunctionResult result = await Function.InvokeAsync(Kernel, arguments, cancellationToken).ConfigureAwait(false);

        Logger.LogKernelFunctionTerminationStrategyInvokedFunction(nameof(ShouldAgentTerminateAsync),
            Function.PluginName,
            Function.Name,
            result.ValueType);

        return ResultParser.Invoke(result);
    }
}
