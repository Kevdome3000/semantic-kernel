// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.FunctionCalling.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using SemanticKernel.AI.ChatCompletion;


/// <summary>
///  OpenAI chat completion extensions for function calling
/// </summary>
public static class OpenAIChatCompletionExtensions
{
    /// <summary>
    /// Generate a function call from a chat history
    /// </summary>
    /// <param name="chatCompletion"></param>
    /// <param name="chat"></param>
    /// <param name="requestSettings"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<string> GenerateFunctionCallAsync(
        this IChatCompletion chatCompletion,
        ChatHistory chat,
        FunctionCallRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IChatResult>? chatResults = await chatCompletion.GetChatCompletionsAsync(chat, requestSettings, cancellationToken).ConfigureAwait(false);
        var firstChatMessage = await chatResults[0].GetChatMessageAsync(cancellationToken).ConfigureAwait(false);
        return firstChatMessage.Content;
    }


    /// <summary>
    /// Generate a new chat message
    /// </summary>
    /// <param name="chatCompletion">Target interface to extend</param>
    /// <param name="chat">Chat history</param>
    /// <param name="requestSettings">AI request settings</param>
    /// <param name="callableFunctions"></param>
    /// <param name="functionCall"></param>
    /// <param name="cancellationToken">Async cancellation token</param>
    /// <remarks>This extension does not support multiple prompt results (Only the first will be returned)</remarks>
    /// <returns>Generated chat message in string format</returns>
    public static async Task<string> GenerateFunctionCallAsync(
        this IChatCompletion chatCompletion,
        ChatHistory chat,
        OpenAIRequestSettings requestSettings,
        IEnumerable<FunctionDefinition> callableFunctions,
        FunctionDefinition? functionCall = null,
        CancellationToken cancellationToken = default)
    {
        var functionCallRequestSettings = requestSettings.ToFunctionCallRequestSettings(callableFunctions, functionCall ?? FunctionDefinition.Auto);
        return await GenerateFunctionCallAsync(chatCompletion, chat, functionCallRequestSettings, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    ///  Returns the content of the first result as a <typeparamref name="T"/> object.
    /// </summary>
    /// <param name="chatCompletion"></param>
    /// <param name="chat"></param>
    /// <param name="requestSettings"></param>
    /// <param name="options"></param>
    /// <param name="deserializationFallback"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<T?> GenerateResponseAsync<T>(
        this IChatCompletion chatCompletion,
        ChatHistory chat,
        FunctionCallRequestSettings requestSettings,
        JsonSerializerOptions? options = null,
        Func<string, T>? deserializationFallback = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IChatResult>? chatResults = await chatCompletion.GetChatCompletionsAsync(chat, requestSettings, cancellationToken).ConfigureAwait(false);
        var firstChatMessage = await chatResults[0].GetChatMessageAsync(cancellationToken).ConfigureAwait(false);
        T? result = default;
        var content = firstChatMessage.Content;

        try
        {

            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var rootJsonString = root.GetRawText().Trim();

            try
            {
                // Try to deserialize the entire response
                result = JsonSerializer.Deserialize<T>(rootJsonString, options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
            }
            catch (JsonException)
            {
                // If the entire response can't be deserialized, try to deserialize the first element
                var propertyEnumerator = root.EnumerateObject();

                if (propertyEnumerator.MoveNext())
                {
                    var firstProperty = propertyEnumerator.Current.Value;
                    var firstElementJsonString = firstProperty.GetRawText().Trim();
                    result = JsonSerializer.Deserialize<T>(firstElementJsonString, options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
                }
            }
        }

        catch (JsonException)
        {
            if (deserializationFallback != null)
            {
                result = deserializationFallback.Invoke(content);
            }
        }

        if (result is not null)
        {
            return result;
        }

        Console.WriteLine($"Error while converting response to a '{typeof(T)}': {nameof(result)} is null. Emitting damaged response.");
        requestSettings.EmitDamagedResponse?.Invoke(content);

        return result;
    }


    /// <summary>
    ///  Returns the content of the first result as a <typeparamref name="T"/> object.
    /// </summary>
    /// <param name="chatCompletion"></param>
    /// <param name="chat"></param>
    /// <param name="requestSettings"></param>
    /// <param name="callableFunctions"></param>
    /// <param name="functionCall"></param>
    /// <param name="options"></param>
    /// <param name="deserializationFallback"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<T?> GenerateResponseAsync<T>(
        this IChatCompletion chatCompletion,
        ChatHistory chat,
        OpenAIRequestSettings requestSettings,
        IEnumerable<FunctionDefinition> callableFunctions,
        FunctionDefinition? functionCall = null,
        JsonSerializerOptions? options = null,
        Func<string, T>? deserializationFallback = null,
        CancellationToken cancellationToken = default)
    {
        var functionCallRequestSettings = requestSettings.ToFunctionCallRequestSettings(callableFunctions, functionCall ?? FunctionDefinition.Auto);
        return await GenerateResponseAsync(chatCompletion, chat, functionCallRequestSettings, options, deserializationFallback, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    ///  Converts the <see cref="OpenAIRequestSettings"/> to <see cref="FunctionCallRequestSettings"/>
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="callableFunctions"></param>
    /// <param name="targetFunctionCall"></param>
    /// <returns></returns>
    public static FunctionCallRequestSettings ToFunctionCallRequestSettings(this OpenAIRequestSettings settings, IEnumerable<FunctionDefinition> callableFunctions, FunctionDefinition? targetFunctionCall = null)
    {
        // Remove duplicates, if any, due to the inaccessibility of ReadOnlySkillCollection
        // Can't changes what skills are available to the context because you can't remove skills from the context
        List<FunctionDefinition> distinctCallableFunctions = callableFunctions
            .GroupBy(func => func.Name)
            .Select(group => group.First())
            .ToList();

        var requestSettings = new FunctionCallRequestSettings
        {
            Temperature = settings.Temperature,
            TopP = settings.TopP,
            PresencePenalty = settings.PresencePenalty,
            FrequencyPenalty = settings.FrequencyPenalty,
            StopSequences = settings.StopSequences,
            MaxTokens = settings.MaxTokens,
            TargetFunctionCall = targetFunctionCall ?? FunctionDefinition.Auto,
            CallableFunctions = distinctCallableFunctions
        };

        return requestSettings;
    }

}
