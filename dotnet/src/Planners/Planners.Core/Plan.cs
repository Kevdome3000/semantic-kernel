// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.Text;

namespace Microsoft.SemanticKernel.Planning;

/// <summary>
/// Standard Semantic Kernel callable plan.
/// Plan is used to create trees of <see cref="KernelFunction"/>s.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class Plan
{
    internal const string MainKey = "INPUT";

    /// <summary>
    /// State of the plan
    /// </summary>
    [JsonPropertyName("state")]
    [JsonConverter(typeof(ContextVariablesConverter))]
    public ContextVariables State { get; } = new();

    /// <summary>
    /// Steps of the plan
    /// </summary>
    [JsonPropertyName("steps")]
    public IReadOnlyList<Plan> Steps => _steps.AsReadOnly();

    /// <summary>
    /// Parameters for the plan, used to pass information to the next step
    /// </summary>
    [JsonPropertyName("parameters")]
    [JsonConverter(typeof(ContextVariablesConverter))]
    public ContextVariables Parameters { get; set; } = new();

    /// <summary>
    /// Outputs for the plan, used to pass information to the caller
    /// </summary>
    [JsonPropertyName("outputs")]
    public IList<string> Outputs { get; set; } = new List<string>();

    /// <summary>
    /// Gets whether the plan has a next step.
    /// </summary>
    [JsonIgnore]
    public bool HasNextStep => NextStepIndex < Steps.Count;

    /// <summary>
    /// Gets the next step index.
    /// </summary>
    [JsonPropertyName("next_step_index")]
    public int NextStepIndex { get; private set; }

    /// <inheritdoc/>
    [JsonPropertyName("plugin_name")]
    public string PluginName { get; set; } = string.Empty;


    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a goal description.
    /// </summary>
    /// <param name="goal">The goal of the plan used as description.</param>
    public Plan(string goal)
    {
        PluginName = nameof(Plan); // TODO markwallace - remove this
        Name = GetRandomPlanName();
        Description = goal;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a goal description and steps.
    /// </summary>
    /// <param name="goal">The goal of the plan used as description.</param>
    /// <param name="steps">The steps to add.</param>
    public Plan(string goal, params KernelFunction[] steps) : this(goal)
    {
        AddSteps(steps);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a goal description and steps.
    /// </summary>
    /// <param name="goal">The goal of the plan used as description.</param>
    /// <param name="steps">The steps to add.</param>
    public Plan(string goal, params Plan[] steps) : this(goal)
    {
        AddSteps(steps);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a function.
    /// </summary>
    /// <param name="function">The function to execute.</param>
    public Plan(KernelFunction function)
    {
        Function = function;
        Name = function.Name;
        Description = function.Description;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="Plan"/> class with a function and steps.
    /// </summary>
    /// <param name="name">The name of the plan.</param>
    /// <param name="pluginName">The name of the plugin.</param>
    /// <param name="description">The description of the plan.</param>
    /// <param name="nextStepIndex">The index of the next step.</param>
    /// <param name="state">The state of the plan.</param>
    /// <param name="parameters">The parameters of the plan.</param>
    /// <param name="outputs">The outputs of the plan.</param>
    /// <param name="steps">The steps of the plan.</param>
    [JsonConstructor]
    public Plan(
        string name,
        string pluginName,
        string description,
        int nextStepIndex,
        ContextVariables state,
        ContextVariables parameters,
        IList<string> outputs,
        IReadOnlyList<Plan> steps)
    {
        PluginName = pluginName; // TODO markwallace - remove this
        Name = name;
        Description = description;
        NextStepIndex = nextStepIndex;
        State = state;
        Parameters = parameters;
        Outputs = outputs;
        _steps.Clear();
        AddSteps(steps.ToArray());
    }


    /// <summary>
    /// Deserialize a JSON string into a Plan object.
    /// TODO: the context should never be null, it's required internally
    /// </summary>
    /// <param name="json">JSON string representation of a Plan</param>
    /// <param name="plugins">The collection of available functions..</param>
    /// <param name="requireFunctions">Whether to require functions to be registered. Only used when context is not null.</param>
    /// <returns>An instance of a Plan object.</returns>
    /// <remarks>If Context is not supplied, plan will not be able to execute.</remarks>
    public static Plan FromJson(string json, IReadOnlyKernelPluginCollection? plugins = null, bool requireFunctions = true)
    {
        Plan plan = JsonSerializer.Deserialize<Plan>(json, s_includeFieldsOptions) ?? new Plan(string.Empty);

        if (plugins != null)
        {
            plan = SetAvailablePlugins(plan, plugins, requireFunctions);
        }

        return plan;
    }


    /// <summary>
    /// Get JSON representation of the plan.
    /// </summary>
    /// <param name="indented">Whether to emit indented JSON</param>
    /// <returns>Plan serialized using JSON format</returns>
    public string ToJson(bool indented = false) =>
        indented ? JsonSerializer.Serialize(this, JsonOptionsCache.WriteIndented) : JsonSerializer.Serialize(this);


    /// <summary>
    /// Adds one or more existing plans to the end of the current plan as steps.
    /// </summary>
    /// <param name="steps">The plans to add as steps to the current plan.</param>
    /// <remarks>
    /// When you add a plan as a step to the current plan, the steps of the added plan are executed after the steps of the current plan have completed.
    /// </remarks>
    public void AddSteps(params Plan[] steps)
    {
        _steps.AddRange(steps);
    }


    /// <summary>
    /// Adds one or more new steps to the end of the current plan.
    /// </summary>
    /// <param name="steps">The steps to add to the current plan.</param>
    /// <remarks>
    /// When you add a new step to the current plan, it is executed after the previous step in the plan has completed. Each step can be a function call or another plan.
    /// </remarks>
    public void AddSteps(params KernelFunction[] steps)
    {
        _steps.AddRange(steps.Select(step => new Plan(step)));
    }


    /// <summary>
    /// Runs the next step in the plan using the provided kernel instance and variables.
    /// </summary>
    /// <param name="kernel">The kernel instance to use for executing the plan.</param>
    /// <param name="variables">The variables to use for the execution of the plan.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task representing the asynchronous execution of the plan's next step.</returns>
    /// <remarks>
    /// This method executes the next step in the plan using the specified kernel instance and context variables.
    /// The context variables contain the necessary information for executing the plan, such as the functions and logger.
    /// The method returns a task representing the asynchronous execution of the plan's next step.
    /// </remarks>
    public Task<Plan> RunNextStepAsync(Kernel kernel, ContextVariables variables, CancellationToken cancellationToken = default) => InvokeNextStepAsync(kernel, variables, cancellationToken);


    /// <summary>
    /// Invoke the next step of the plan
    /// </summary>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    /// <param name="variables">Context variables to use</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The updated plan</returns>
    /// <exception cref="KernelException">If an error occurs while running the plan</exception>
    public async Task<Plan> InvokeNextStepAsync(Kernel kernel, ContextVariables variables, CancellationToken cancellationToken = default)
    {
        if (HasNextStep)
        {
            await InternalInvokeNextStepAsync(kernel, variables, cancellationToken).ConfigureAwait(false);
        }

        return this;
    }


    #region ISKFunction implementation

    /// <summary>
    /// Gets the name of the function.
    /// </summary>
    /// <remarks>
    /// The name is used anywhere the function needs to be identified, such as in plans describing what functions
    /// should be invoked when, or as part of lookups in a plugin's function collection. Function names are generally
    /// handled in an ordinal case-insensitive manner.
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Gets a description of the function.
    /// </summary>
    /// <remarks>
    /// The description may be supplied to a model in order to elaborate on the function's purpose,
    /// in case it may be beneficial for the model to recommend invoking the function.
    /// </remarks>
    public string Description { get; }


    /// <summary>
    /// Gets the metadata describing the function.
    /// </summary>
    /// <returns>An instance of <see cref="KernelFunctionMetadata"/> describing the function</returns>
    public KernelFunctionMetadata GetMetadata()
    {
        if (Function is not null)
        {
            return Function.Metadata;
        }

        // The parameter mapping definitions from Plan -> Function
        var stepParameters = Steps.SelectMany(s => s.Parameters);

        // The parameter descriptions from the Function
        IEnumerable<KernelParameterMetadata> stepDescriptions = Steps.SelectMany(s => s.GetMetadata().Parameters);

        // The parameters for the Plan
        var parameters = Parameters.Select(p =>
        {
            var matchingParameter = stepParameters.FirstOrDefault(sp => sp.Value.Equals($"${p.Key}", StringComparison.OrdinalIgnoreCase));
            KernelParameterMetadata? stepDescription = stepDescriptions.FirstOrDefault(sd => sd.Name.Equals(matchingParameter.Key, StringComparison.OrdinalIgnoreCase));

            return new KernelParameterMetadata(p.Key)
            {
                Description = stepDescription?.Description,
                DefaultValue = stepDescription?.DefaultValue,
                IsRequired = stepDescription?.IsRequired ?? false,
                ParameterType = stepDescription?.ParameterType,
                Schema = stepDescription?.Schema
            };
        }).ToList();

        return new KernelFunctionMetadata(Name)
        {
            PluginName = PluginName,
            Description = Description,
            Parameters = parameters
        };
    }


    /// <summary>
    /// Invoke the <see cref="KernelFunction"/>.
    /// </summary>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    /// <param name="input">Plan input</param>
    public async Task<FunctionResult> InvokeAsync(
        Kernel kernel,
        string input)
    {
        var contextVariables = new ContextVariables();
        contextVariables.Update(input);

        return await InvokeAsync(kernel, contextVariables).ConfigureAwait(false);
    }


    /// <summary>
    /// Invoke the <see cref="KernelFunction"/>.
    /// </summary>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    /// <param name="variables">Context variables</param>
    /// <param name="executionSettings">LLM completion settings (for semantic functions only)</param>
    /// <returns>The updated context, potentially a new one if context switching is implemented.</returns>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    public async Task<FunctionResult> InvokeAsync(
        Kernel kernel,
        ContextVariables? variables = null,
        PromptExecutionSettings? executionSettings = null,
        CancellationToken cancellationToken = default)
    {
        variables ??= new ContextVariables();
        FunctionResult? result = new FunctionResult(Name, variables);

        if (Function is not null)
        {
            // Merge state with the current context variables.
            // Then filter the variables to only those needed for the next step.
            // This is done to prevent the function from having access to variables that it shouldn't.
            AddStateVariablesToContextVariables(State, variables);

            var functionVariables = GetNextStepVariables(variables, this);

            // Execute the step
            result = await Function
                .InvokeAsync(kernel, functionVariables, executionSettings, cancellationToken)
                .ConfigureAwait(false);
            UpdateFunctionResultWithOutputs(result);
        }
        else
        {
            // loop through steps and execute until completion
            while (HasNextStep)
            {
                AddStateVariablesToContextVariables(State, variables);

                FunctionResult? stepResult = await InternalInvokeNextStepAsync(kernel, variables, cancellationToken).ConfigureAwait(false);

                // If a step was cancelled before invocation
                // Return the last result state of the plan.
                if (stepResult.IsCancellationRequested)
                {
                    return result;
                }

                if (stepResult.IsSkipRequested)
                {
                    continue;
                }

                UpdateContextWithOutputs(variables);

                result = new FunctionResult(Name, variables, variables.Input);
                UpdateFunctionResultWithOutputs(result);
            }
        }

        return result;
    }

    #endregion ISKFunction implementation


    /// <summary>
    /// Expand variables in the input string.
    /// </summary>
    /// <param name="variables">Variables to use for expansion.</param>
    /// <param name="input">Input string to expand.</param>
    /// <returns>Expanded string.</returns>
    internal string ExpandFromVariables(ContextVariables variables, string input)
    {
        string result = input;
        MatchCollection matches = s_variablesRegex.Matches(input);
        IOrderedEnumerable<string> orderedMatches = matches.Cast<Match>().Select(m => m.Groups["var"].Value).Distinct().OrderByDescending(m => m.Length);

        foreach (string? varName in orderedMatches)
        {
            if (variables.TryGetValue(varName, out string? value) || State.TryGetValue(varName, out value))
            {
                result = result.Replace($"${varName}", value);
            }
        }

        return result;
    }


    /// <summary>
    /// Invoke the next step of the plan
    /// </summary>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    /// <param name="variables">Context variables to use</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>Next step result</returns>
    /// <exception cref="KernelException">If an error occurs while running the plan</exception>
    private async Task<FunctionResult> InternalInvokeNextStepAsync(Kernel kernel, ContextVariables variables, CancellationToken cancellationToken = default)
    {
        if (HasNextStep)
        {
            Plan? step = Steps[NextStepIndex];

            // Merge the state with the current context variables for step execution
            var functionVariables = GetNextStepVariables(variables, step);

            // Execute the step
            FunctionResult? result = await step.InvokeAsync(kernel, functionVariables, null, cancellationToken).ConfigureAwait(false);

            string resultValue = (result.TryGetVariableValue(MainKey, out string? value) ? value : string.Empty).Trim();


            #region Update State

            // Update state with result
            State.Update(resultValue);

            // Update Plan Result in State with matching outputs (if any)
            if (Outputs.Intersect(step.Outputs).Any())
            {
                if (State.TryGetValue(DefaultResultKey, out string? currentPlanResult))
                {
                    State.Set(DefaultResultKey, $"{currentPlanResult}\n{resultValue}");
                }
                else
                {
                    State.Set(DefaultResultKey, resultValue);
                }
            }

            // Update state with outputs (if any)
            foreach (string? item in step.Outputs)
            {
                if (result.TryGetVariableValue(item, out string? val))
                {
                    State.Set(item, val);
                }
                else
                {
                    State.Set(item, resultValue);
                }
            }

            #endregion Update State


            NextStepIndex++;

            return result;
        }

        throw new InvalidOperationException("There isn't a next step");
    }


    /// <summary>
    /// Set functions for a plan and its steps.
    /// </summary>
    /// <param name="plan">Plan to set functions for.</param>
    /// <param name="plugins">The collection of available plugins.</param>
    /// <param name="requireFunctions">Whether to throw an exception if a function is not found.</param>
    /// <returns>The plan with functions set.</returns>
    private static Plan SetAvailablePlugins(Plan plan, IReadOnlyKernelPluginCollection plugins, bool requireFunctions = true)
    {
        if (plan.Steps.Count == 0)
        {
            Verify.NotNull(plugins);

            if (plugins.TryGetFunction(plan.PluginName, plan.Name, out KernelFunction? planFunction))
            {
                plan.Function = planFunction;
            }
            else if (requireFunctions)
            {
                throw new KernelException($"Function '{plan.PluginName}.{plan.Name}' not found in function collection");
            }
        }
        else
        {
            foreach (Plan? step in plan.Steps)
            {
                SetAvailablePlugins(step, plugins, requireFunctions);
            }
        }

        return plan;
    }


    /// <summary>
    /// Add any missing variables from a plan state variables to the context.
    /// </summary>
    private static void AddStateVariablesToContextVariables(ContextVariables vars, ContextVariables contextVariables)
    {
        // Loop through vars and add anything missing to context
        foreach (var item in vars)
        {
            if (!contextVariables.TryGetValue(item.Key, out string? value) || string.IsNullOrEmpty(value))
            {
                contextVariables.Set(item.Key, item.Value);
            }
        }
    }


    /// <summary>
    /// Update the context with the outputs from the current step.
    /// </summary>
    /// <param name="variables">The context variables to update.</param>
    /// <returns>The updated context variables.</returns>
    private ContextVariables UpdateContextWithOutputs(ContextVariables variables)
    {
        string? resultString = State.TryGetValue(DefaultResultKey, out string? result) ? result : State.ToString();
        variables.Update(resultString);

        // copy previous step's variables to the next step
        foreach (string? item in _steps[NextStepIndex - 1].Outputs)
        {
            if (State.TryGetValue(item, out string? val))
            {
                variables.Set(item, val);
            }
            else
            {
                variables.Set(item, resultString);
            }
        }

        return variables;
    }


    /// <summary>
    /// Update the function result with the outputs from the current state.
    /// </summary>
    /// <param name="functionResult">The function result to update.</param>
    /// <returns>The updated function result.</returns>
    private FunctionResult? UpdateFunctionResultWithOutputs(FunctionResult? functionResult)
    {
        if (functionResult is null)
        {
            return null;
        }

        foreach (string? output in Outputs)
        {
            if (State.TryGetValue(output, out var value))
            {
                functionResult.Metadata[output] = value;
            }
            else if (functionResult.TryGetVariableValue(output, out var val))
            {
                functionResult.Metadata[output] = val;
            }
        }

        return functionResult;
    }


    /// <summary>
    /// Get the variables for the next step in the plan.
    /// </summary>
    /// <param name="variables">The current context variables.</param>
    /// <param name="step">The next step in the plan.</param>
    /// <returns>The context variables for the next step in the plan.</returns>
    private ContextVariables GetNextStepVariables(ContextVariables variables, Plan step)
    {
        // Priority for Input
        // - Parameters (expand from variables if needed)
        // - SKContext.Variables
        // - Plan.State
        // - Empty if sending to another plan
        // - Plan.Description

        string? input = string.Empty;

        if (!string.IsNullOrEmpty(step.Parameters.Input))
        {
            input = ExpandFromVariables(variables, step.Parameters.Input!);
        }
        else if (!string.IsNullOrEmpty(variables.Input))
        {
            input = variables.Input;
        }
        else if (!string.IsNullOrEmpty(State.Input))
        {
            input = State.Input;
        }
        else if (step.Steps.Count > 0)
        {
            input = string.Empty;
        }
        else if (!string.IsNullOrEmpty(Description))
        {
            input = Description;
        }

        var stepVariables = new ContextVariables(input);

        // Priority for remaining stepVariables is:
        // - Function Parameters (pull from variables or state by a key value)
        // - Step Parameters (pull from variables or state by a key value)
        // - All other variables. These are carried over in case the function wants access to the ambient content.
        KernelFunctionMetadata functionParameters = step.GetMetadata();

        foreach (KernelParameterMetadata? param in functionParameters.Parameters)
        {
            if (param.Name.Equals(MainKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (variables.TryGetValue(param.Name, out string? value))
            {
                stepVariables.Set(param.Name, value);
            }
            else if (State.TryGetValue(param.Name, out value) && !string.IsNullOrEmpty(value))
            {
                stepVariables.Set(param.Name, value);
            }
        }

        foreach (var item in step.Parameters)
        {
            // Don't overwrite variable values that are already set
            if (stepVariables.ContainsKey(item.Key))
            {
                continue;
            }

            string expandedValue = ExpandFromVariables(variables, item.Value);

            if (!expandedValue.Equals(item.Value, StringComparison.OrdinalIgnoreCase))
            {
                stepVariables.Set(item.Key, expandedValue);
            }
            else if (variables.TryGetValue(item.Key, out string? value))
            {
                stepVariables.Set(item.Key, value);
            }
            else if (State.TryGetValue(item.Key, out value))
            {
                stepVariables.Set(item.Key, value);
            }
            else
            {
                stepVariables.Set(item.Key, expandedValue);
            }
        }

        foreach (KeyValuePair<string, string> item in variables)
        {
            if (!stepVariables.ContainsKey(item.Key))
            {
                stepVariables.Set(item.Key, item.Value);
            }
        }

        return stepVariables;
    }


    private static string GetRandomPlanName() => "plan" + Guid.NewGuid().ToString("N");

    /// <summary>Deserialization options for including fields.</summary>
    private static readonly JsonSerializerOptions s_includeFieldsOptions = new() { IncludeFields = true };

    private KernelFunction? Function { get; set; }

    private readonly List<Plan> _steps = new();

    private static readonly Regex s_variablesRegex = new(@"\$(?<var>\w+)");

    private const string DefaultResultKey = "PLAN.RESULT";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            string display = Description;

            if (!string.IsNullOrWhiteSpace(Name))
            {
                display = $"{Name} ({display})";
            }

            if (_steps.Count > 0)
            {
                display += $", Steps = {_steps.Count}, NextStep = {NextStepIndex}";
            }

            return display;
        }
    }
}
