// Copyright (c) Microsoft. All rights reserved.
using System.Text.RegularExpressions;

namespace Microsoft.SemanticKernel.Agents.Chat;

/// <summary>
/// Signals termination when the most recent message matches against the defined regular expressions
/// for the specified agent (if provided).
/// </summary>
[Experimental("SKEXP0110")]
public sealed class RegexTerminationStrategy : TerminationStrategy
{

    private readonly Regex[] _expressions;


    /// <summary>
    /// Initializes a new instance of the <see cref="RegexTerminationStrategy"/> class.
    /// </summary>
    /// <param name="expressions">
    /// A list of regular expressions to match against an agent's last message to
    /// determine whether processing should terminate.
    /// </param>
    public RegexTerminationStrategy(params string[] expressions)
    {
        Verify.NotNull(expressions);

        _expressions = expressions.Where(s => s is not null).Select(e => new Regex(e, RegexOptions.Compiled)).ToArray();
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="RegexTerminationStrategy"/> class.
    /// </summary>
    /// <param name="expressions">
    /// A list of regular expressions to match against an agent's last message to
    /// determine whether processing should terminate.
    /// </param>
    public RegexTerminationStrategy(params Regex[] expressions)
    {
        Verify.NotNull(expressions);

        _expressions = expressions;
    }


    /// <inheritdoc/>
    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
    {
        // Most recent message
        if (history.Count > 0 && history[history.Count - 1].Content is string message)
        {
            Logger.LogRegexTerminationStrategyEvaluating(nameof(ShouldAgentTerminateAsync), _expressions.Length);

            // Evaluate expressions for match
            foreach (var expression in _expressions)
            {
                Logger.LogRegexTerminationStrategyEvaluatingExpression(nameof(ShouldAgentTerminateAsync), expression);

                if (expression.IsMatch(message))
                {
                    Logger.LogRegexTerminationStrategyMatchedExpression(nameof(ShouldAgentTerminateAsync), expression);

                    return Task.FromResult(true);
                }
            }
        }

        Logger.LogRegexTerminationStrategyNoMatch(nameof(ShouldAgentTerminateAsync));

        return Task.FromResult(false);
    }

}
