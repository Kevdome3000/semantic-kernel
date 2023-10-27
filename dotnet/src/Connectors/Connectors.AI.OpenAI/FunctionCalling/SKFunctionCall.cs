// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.FunctionCalling;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Diagnostics;
using Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orchestration;
using SemanticKernel.AI;
using SemanticKernel.AI.ChatCompletion;
using SemanticKernel.AI.TextCompletion;
using TemplateEngine;

#pragma warning disable format
/// <summary>
/// A semantic function that calls other functions
/// </summary>
public sealed class SKFunctionCall : ISKFunction
{

    /// <summary>
    /// The maximum number of callable functions to include in a FunctionCall request
    /// </summary>
    public const int MaxCallableFunctions = 64;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string PluginName { get; }

    /// <inheritdoc />
    public string SkillName { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public bool IsSemantic => true;

    /// <inheritdoc />
    public AIRequestSettings RequestSettings { get; private set; }

    /// <summary>
    ///  The callable functions for this SKFunctionCall instance
    /// </summary>
    public List<FunctionDefinition> CallableFunctions { get; }

    /// <summary>
    ///  Whether to call execute the function call automatically
    /// </summary>
    public bool CallFunctionsAutomatically { get; }


    /// <summary>
    /// Initializes a new instance of <see cref="SKFunctionCall"/>.
    /// </summary>
    /// <param name="skillName"></param>
    /// <param name="functionName"></param>
    /// <param name="functionConfig"></param>
    /// <param name="loggerFactory"></param>
    /// <returns></returns>
    public static ISKFunction FromConfig(
        string skillName,
        string functionName,
        SKFunctionCallConfig functionConfig,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(functionConfig);

        var func = new SKFunctionCall(
            functionConfig.PromptTemplate,
            functionConfig.PromptTemplateConfig.GetDefaultRequestSettings(),
            skillName,
            functionName,
            functionConfig.PromptTemplateConfig.Description,
            functionConfig.TargetFunction,
            functionConfig.CallableFunctions,
            functionConfig.CallFunctionsAutomatically,
            loggerFactory
        );

        return func;
    }


    /// <inheritdoc />
    public FunctionView Describe()
    {
        return new FunctionView(Name, PluginName, Description, CallableFunctions.Select(f => new ParameterView(f.Name, f.Description)).ToList());
    }


    /// <inheritdoc />
    public async Task<FunctionResult> InvokeAsync(SKContext context, AIRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
    {
        SetCallableFunctions(context.Functions);

        IChatCompletion? chatCompletion = context.ServiceProvider.GetService<IChatCompletion>();

        if(chatCompletion is null)
        {
            throw new SKException("The chat completion service is null");
        }

        var settings = GetRequestSettings(requestSettings ?? RequestSettings);

        // trim any skills from the 
        var result = await RunPromptAsync(chatCompletion, settings, context, cancellationToken).ConfigureAwait(false);

        if (!CallFunctionsAutomatically)
        {
            return result;
        }

        var functionCallResult = await ExecuteFunctionCallAsync(result, context, cancellationToken).ConfigureAwait(false);

        if (functionCallResult is null)
        {
            throw new SKException("The function call result is null");
        }
        return functionCallResult;

    }


    private void SetCallableFunctions(IReadOnlyFunctionCollection functions)
    {
        if (_targetFunctionDefinition != FunctionDefinition.Auto)
        {
            CallableFunctions.Add(_targetFunctionDefinition);
        }

        List<FunctionDefinition> functionDefinitions = functions.GetFunctionDefinitions(new[] { PluginName }).Take(MaxCallableFunctions - 1).ToList();
        //for each functionDefinition not in callableFunctions, add it to the list
        CallableFunctions.AddRange(functionDefinitions.Where(functionDefinition => CallableFunctions.TrueForAll(f => f.Name != functionDefinition.Name)));
    }
    /// <inheritdoc />
    public ISKFunction SetDefaultFunctionCollection(IReadOnlyFunctionCollection functions) => this;


    /// <inheritdoc />
    public ISKFunction SetDefaultSkillCollection(IReadOnlyFunctionCollection skills) => SetDefaultFunctionCollection(skills);


    /// <inheritdoc />
    public ISKFunction SetAIService(Func<ITextCompletion> serviceFactory) => this;


    /// <inheritdoc/>
    public ISKFunction SetAIConfiguration(AIRequestSettings? requestSettings)
    {
        Verify.NotNull(requestSettings);
        RequestSettings = requestSettings;
        return this;
    }

    internal SKFunctionCall(
        IPromptTemplate template,
        AIRequestSettings requestSettings,
        string skillName,
        string functionName,
        string description,
        FunctionDefinition? targetFunctionDefinition = null,
        List<FunctionDefinition>? callableFunctions = null,
        bool callFunctionsAutomatically = true,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(template);
        Verify.ValidPluginName(skillName);
        Verify.ValidFunctionName(functionName);

        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(SKFunctionCall)) : NullLogger.Instance;

        RequestSettings = requestSettings;
        _promptTemplate = template;
        Name = functionName;
        PluginName = skillName;
        SkillName = skillName;
        Description = description;
        _targetFunctionDefinition = targetFunctionDefinition ?? FunctionExtensions.Default;
        CallFunctionsAutomatically = callFunctionsAutomatically;
        CallableFunctions = callableFunctions ?? new List<FunctionDefinition>();

        if (_targetFunctionDefinition != FunctionDefinition.Auto)
        {
            CallableFunctions.Add(_targetFunctionDefinition);
        }
    }


    #region private

    private readonly ILogger _logger;
    private readonly FunctionDefinition _targetFunctionDefinition;
    private readonly IPromptTemplate _promptTemplate;


    private FunctionCallRequestSettings GetRequestSettings(AIRequestSettings settings)
    {

        var openAIRequestSettings = OpenAIRequestSettings.FromRequestSettings(settings);
        // Remove duplicates, if any, due to the inaccessibility of ReadOnlySkillCollection
        // Can't changes what skills are available to the context because you can't remove skills from the context
        List<FunctionDefinition> distinctCallableFunctions = CallableFunctions
            .GroupBy(func => func.Name)
            .Select(group => group.First())
            .ToList();

        var requestSettings = new FunctionCallRequestSettings
        {
            Temperature = openAIRequestSettings.Temperature,
            TopP = openAIRequestSettings.TopP,
            PresencePenalty = openAIRequestSettings.PresencePenalty,
            FrequencyPenalty = openAIRequestSettings.FrequencyPenalty,
            StopSequences = openAIRequestSettings.StopSequences,
            MaxTokens = openAIRequestSettings.MaxTokens,
            TargetFunctionCall = _targetFunctionDefinition,
            CallableFunctions = distinctCallableFunctions
        };

        return requestSettings;
    }


    private static async Task<string> GetCompletionsResultContentAsync(IReadOnlyList<IChatResult> completions, CancellationToken cancellationToken = default)
    {
        // To avoid any unexpected behavior we only take the first completion result (when running from the Kernel)
        var message = await completions[0].GetChatMessageAsync(cancellationToken).ConfigureAwait(false);
        return message.Content;
    }


    private async Task<FunctionResult> RunPromptAsync(
        IChatCompletion? client,
        FunctionCallRequestSettings? requestSettings,
        SKContext context,
        CancellationToken cancellationToken)
    {
        Verify.NotNull(client);
        Verify.NotNull(requestSettings);

        FunctionResult result;

        try
        {
            var renderedPrompt = await _promptTemplate.RenderAsync(context, cancellationToken).ConfigureAwait(false);
            requestSettings.ChatSystemPrompt = renderedPrompt;
            _logger.LogInformation("SKFunctionCall: Calling function {Plugin}.{Name} with prompt {Prompt}", PluginName, Name, renderedPrompt);
            IReadOnlyList<IChatResult> completionResults = await client.GetChatCompletionsAsync(client.CreateNewChat(renderedPrompt), requestSettings, cancellationToken).ConfigureAwait(false);

            if (completionResults is null)
            {
                throw new SKException("The completion results are null");
            }

            var completion = await GetCompletionsResultContentAsync(completionResults, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("SKFunctionCall: Function {Plugin}.{Name} completed with result {Result}", PluginName, Name, completion);
            // Update the result with the completion
            context.Variables.Update(completion);

            result = new FunctionResult(Name, PluginName, context, completion);

            ModelResult[] modelResults = completionResults.Select(c => c.ModelResult).ToArray();

            result.Metadata.Add("ModelResults", modelResults);
        }
        catch (Exception ex) when (!ex.IsCriticalException())
        {
            _logger.LogError(ex, "Semantic function {Plugin}.{Name} execution failed with error {Error}", PluginName, Name, ex.Message);
            throw;
        }

        return result;
    }


    private async Task<FunctionResult?> ExecuteFunctionCallAsync(FunctionResult result, SKContext context, CancellationToken cancellationToken)
    {
        var functionCallResult = result.ToFunctionCallResult();

        if (functionCallResult is null)
        {
            throw new SKException("The function call result is null");
        }

        if (!context.Functions.TryGetFunction(functionCallResult, out ISKFunction? functionToCall))
        {
            throw new SKException($"The function {functionCallResult.Function} is not available");
        }

        // Update the result with the completion
        foreach (KeyValuePair<string, string> item in functionCallResult.FunctionParameters())
        {
            context.Variables[item.Key] = item.Value;
        }

        if (functionToCall != null)
        {
            return await functionToCall.InvokeAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    #endregion


}
