// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Experimental.Assistants.Internal;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Extensions;
using Models;
using Orchestration;


/// <summary>
/// Represents an execution run on a thread.
/// </summary>
internal sealed class ChatRun
{
    /// <inheritdoc/>
    public string Id => _model.Id;

    /// <inheritdoc/>
    public string AssistantId => _model.AssistantId;

    /// <inheritdoc/>
    public string ThreadId => _model.ThreadId;

    private const string ActionState = "requires_action";
    private const string FailedState = "failed";
    private static readonly TimeSpan s_pollingInterval = TimeSpan.FromMilliseconds(200);

    private static readonly HashSet<string> s_pollingStates =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "queued",
            "in_progress"
        };

    private readonly OpenAIRestContext _restContext;
    private ThreadRunModel _model;
    private readonly Kernel _kernel;


    /// <inheritdoc/>
    public async Task<IList<string>> GetResultAsync(CancellationToken cancellationToken = default)
    {
        // Poll until actionable
        await PollRunStatus().ConfigureAwait(false);

        // Retrieve steps
        var steps = await _restContext.GetRunStepsAsync(ThreadId, Id, cancellationToken).ConfigureAwait(false);

        // Is tool action required?
        if (ActionState.Equals(_model.Status, StringComparison.OrdinalIgnoreCase))
        {
            // Execute functions in parallel and post results at once.
            var tasks = steps.Data.SelectMany(step => ExecuteStep(step, cancellationToken)).ToArray();
            await Task.WhenAll(tasks).ConfigureAwait(false);

            var results = tasks.Select(t => t.Result).ToArray();
            await _restContext.AddToolOutputsAsync(ThreadId, Id, results, cancellationToken).ConfigureAwait(false);

            // Refresh run as it goes back into pending state after posting function results.
            _model = await _restContext.GetRunAsync(ThreadId, Id, cancellationToken).ConfigureAwait(false);
            await PollRunStatus().ConfigureAwait(false);

            // Refresh steps to retrieve additional messages.
            steps = await _restContext.GetRunStepsAsync(ThreadId, Id, cancellationToken).ConfigureAwait(false);
        }

        // Did fail?
        if (FailedState.Equals(_model.Status, StringComparison.OrdinalIgnoreCase))
        {
            throw new SKException($"Unexpected failure processing run: {Id}: {_model.LastError?.Message ?? "Unknown"}");
        }

        var messageIds =
            steps.Data
                .Where(s => s.StepDetails.MessageCreation != null)
                .Select(s => s.StepDetails.MessageCreation!.MessageId)
                .ToArray();

        return messageIds;

        async Task PollRunStatus()
        {
            while (s_pollingStates.Contains(_model.Status))
            {
                await Task.Delay(s_pollingInterval, cancellationToken).ConfigureAwait(false);

                try
                {
                    _model = await _restContext.GetRunAsync(ThreadId, Id, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (!exception.IsCriticalException())
                {
                    // Retry anyway..
                }
            }
        }
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="ChatRun"/> class.
    /// </summary>
    internal ChatRun(
        ThreadRunModel model,
        Kernel kernel,
        OpenAIRestContext restContext)
    {
        _model = model;
        _kernel = kernel;
        _restContext = restContext;
    }


    private IEnumerable<Task<ToolResultModel>> ExecuteStep(ThreadRunStepModel step, CancellationToken cancellationToken)
    {
        // Process all of the steps that require action
        if (step.Status == "in_progress" && step.StepDetails.Type == "tool_calls")
        {
            foreach (var toolCall in step.StepDetails.ToolCalls)
            {
                // Run function
                yield return ProcessFunctionStepAsync(toolCall.Id, toolCall.Function, cancellationToken);
            }
        }
    }


    private async Task<ToolResultModel> ProcessFunctionStepAsync(string callId, ThreadRunStepModel.FunctionDetailsModel functionDetails, CancellationToken cancellationToken)
    {
        var result = await InvokeFunctionCallAsync().ConfigureAwait(false);

        return
            new ToolResultModel
            {
                CallId = callId,
                Output = result
            };

        async Task<string> InvokeFunctionCallAsync()
        {
            var function = _kernel.GetAssistantTool(functionDetails.Name);

            var variables = new ContextVariables();

            if (!string.IsNullOrWhiteSpace(functionDetails.Arguments))
            {
                var arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(functionDetails.Arguments)!;

                foreach (var argument in arguments)
                {
                    variables[argument.Key] = argument.Value.ToString();
                }
            }

            var results = await _kernel.RunAsync(function, variables, cancellationToken).ConfigureAwait(false);

            return results.GetValue<string>() ?? string.Empty;
        }
    }
}
