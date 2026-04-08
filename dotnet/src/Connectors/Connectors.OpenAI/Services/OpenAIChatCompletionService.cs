// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;
using OpenAI;

#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
#pragma warning disable RCS1155 // Use StringComparison when comparing strings

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

/// <summary>
/// OpenAI chat completion service.
/// </summary>
public sealed class OpenAIChatCompletionService : IChatCompletionService, ITextGenerationService
{
    /// <summary>Core implementation shared by OpenAI clients.</summary>
    private readonly ClientCore _client;


    /// <summary>
    /// Create an instance of the OpenAI chat completion connector
    /// </summary>
    /// <param name="modelId">Model name</param>
    /// <param name="apiKey">OpenAI API Key</param>
    /// <param name="organization">OpenAI Organization Id (usually optional)</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public OpenAIChatCompletionService(
        string modelId,
        string apiKey,
        string? organization = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null
    )
    {
        _client = new ClientCore(
            modelId,
            apiKey,
            organization,
            null,
            httpClient,
            loggerFactory?.CreateLogger(typeof(OpenAIChatCompletionService)));
    }


    /// <summary>
    /// Create an instance of the Custom Message API OpenAI chat completion connector
    /// </summary>
    /// <param name="modelId">Model name</param>
    /// <param name="endpoint">Custom Message API compatible endpoint</param>
    /// <param name="apiKey">OpenAI API Key</param>
    /// <param name="organization">OpenAI Organization Id (usually optional)</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    [Experimental("SKEXP0010")]
    public OpenAIChatCompletionService(
        string modelId,
        Uri endpoint,
        string? apiKey = null,
        string? organization = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        _client = new ClientCore(
            modelId,
            apiKey,
            organization,
            endpoint ?? httpClient?.BaseAddress,
            httpClient,
            loggerFactory?.CreateLogger(typeof(OpenAIChatCompletionService)));
    }


    /// <summary>
    /// Create an instance of the OpenAI chat completion connector
    /// </summary>
    /// <param name="modelId">Model name</param>
    /// <param name="openAIClient">Custom <see cref="OpenAIClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public OpenAIChatCompletionService(
        string modelId,
        OpenAIClient openAIClient,
        ILoggerFactory? loggerFactory = null)
    {
        _client = new ClientCore(
            modelId,
            openAIClient,
            loggerFactory?.CreateLogger(typeof(OpenAIChatCompletionService)));
    }


    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Attributes => _client.Attributes;


    /// <inheritdoc/>
    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        return _client.GetChatMessageContentsAsync(_client.ModelId,
            chatHistory,
            executionSettings,
            kernel,
            cancellationToken);
    }


    /// <inheritdoc/>
    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        return _client.GetStreamingChatMessageContentsAsync(_client.ModelId,
            chatHistory,
            executionSettings,
            kernel,
            cancellationToken);
    }


    /// <inheritdoc/>
    public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        return _client.GetChatAsTextContentsAsync(_client.ModelId,
            prompt,
            executionSettings,
            kernel,
            cancellationToken);
    }


    /// <inheritdoc/>
    public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        return _client.GetChatAsTextStreamingContentsAsync(_client.ModelId,
            prompt,
            executionSettings,
            kernel,
            cancellationToken);
    }
}
