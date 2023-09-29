// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.FunctionCalling.Extensions;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Orchestration;
using SemanticFunctions;
using SemanticKernel.AI.TextCompletion;


/// <summary>
///  Extensions for creating function calls via the kernel
/// </summary>
public static class SKFunctionCallExtensions
{
    /// <summary>
    ///  Create a function call from a config
    /// </summary>
    /// <param name="kernel"></param>
    /// <param name="promptTemplate"></param>
    /// <param name="functionName"></param>
    /// <param name="skillName"></param>
    /// <param name="description"></param>
    /// <param name="targetFunction"></param>
    /// <param name="callableFunctions"></param>
    /// <param name="callFunctionsAutomatically"></param>
    /// <param name="maxTokens"></param>
    /// <param name="temperature"></param>
    /// <param name="topP"></param>
    /// <param name="presencePenalty"></param>
    /// <param name="frequencyPenalty"></param>
    /// <param name="stopSequences"></param>
    /// <param name="loggerFactory"></param>
    /// <returns></returns>
    public static ISKFunction CreateFunctionCall(
        this IKernel kernel,
        string promptTemplate,
        string? functionName = null,
        string? skillName = null,
        string? description = null,
        FunctionDefinition? targetFunction = null,
        IEnumerable<FunctionDefinition>? callableFunctions = null,
        bool callFunctionsAutomatically = true,
        int? maxTokens = null,
        double temperature = 0,
        double topP = 0,
        double presencePenalty = 0,
        double frequencyPenalty = 0,
        IEnumerable<string>? stopSequences = null,
        ILoggerFactory? loggerFactory = null)
    {
        functionName ??= RandomFunctionName();

        var config = new PromptTemplateConfig
        {
            Description = description ?? "Function call",
            Type = "completion",
            Completion = new OpenAIRequestSettings()
            {
                MaxTokens = maxTokens,
                Temperature = temperature,
                TopP = topP,
                PresencePenalty = presencePenalty,
                FrequencyPenalty = frequencyPenalty,
                StopSequences = stopSequences?.ToList() ?? new List<string>()
            }
        };

        var template = new PromptTemplate(promptTemplate, config, kernel.PromptTemplateEngine);

        SKFunctionCallConfig functionConfig = new(template, config, targetFunction, callableFunctions, callFunctionsAutomatically);
        var functionCall = SKFunctionCall.FromConfig(skillName ?? "sk_function_call", functionName, functionConfig, loggerFactory);
        functionCall.SetAIService(() => kernel.GetService<ITextCompletion>());
        functionCall.SetDefaultFunctionCollection(kernel.Functions);
        return functionCall;
    }


    /// <summary>
    /// Call an SKFunctionCall instance directly and return the result in the specified type
    /// </summary>
    /// <param name="function"></param>
    /// <param name="kernel"></param>
    /// <param name="context"></param>
    /// <param name="serializerOptions"></param>
    /// <param name="deserializationFallback"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<T?> GetFunctionResult<T>(
        this SKFunctionCall function,
        IKernel kernel,
        ContextVariables context,
        JsonSerializerOptions? serializerOptions = null,
        Func<string, T>? deserializationFallback = null,
        CancellationToken cancellationToken = default)
    {
        var functionResult = await function.InvokeAsync(kernel, context, cancellationToken: cancellationToken).ConfigureAwait(false);
        T? result = default;

        try
        {
            result = functionResult.GetValue<T>();
        }

        catch (InvalidCastException exception)
        {
            try
            {
                var resultJson = functionResult.GetValue<string>();
                if (resultJson != null)
                {
                    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(resultJson));
                    result = await JsonSerializer.DeserializeAsync<T>(stream, serializerOptions, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error while converting '{functionResult.Context.Result}' to a '{typeof(T)}': {ex}");

                if (deserializationFallback != null)
                {
                    result = deserializationFallback.Invoke(functionResult.Context.Result);
                }
            }

        }

        return result;
    }


    /// <summary>
    /// Returns the content of the chat message as a FunctionCallResult
    /// </summary>
    /// <param name="functionResult"></param>
    /// <returns></returns>
    public static FunctionCallResult? ToFunctionCallResult(this FunctionResult functionResult)
    {
        FunctionCallResult? functionCall = default;

        try
        {
            functionCall = functionResult.Context.ToFunctionCallResult<FunctionCallResult>();

        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error while converting '{functionResult.Context.Result}' to a '{typeof(FunctionCallResult)}': {ex}");
        }

        return functionCall;
    }


    private static string RandomFunctionName() => "functionCall" + Guid.NewGuid().ToString("N");
}
