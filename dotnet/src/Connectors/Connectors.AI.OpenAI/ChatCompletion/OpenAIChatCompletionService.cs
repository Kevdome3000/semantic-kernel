﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Extensions.Logging;
using SemanticKernel.AI;
using SemanticKernel.AI.ChatCompletion;
using SemanticKernel.AI.TextCompletion;
using Services;


/// <summary>
/// OpenAI chat completion service.
/// </summary>
public sealed class OpenAIChatCompletionService : IChatCompletionService, ITextCompletion
{
    private readonly OpenAIClientCore _core;


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
        ILoggerFactory? loggerFactory = null)
    {
        this._core = new(modelId, apiKey, organization, httpClient, loggerFactory?.CreateLogger(typeof(OpenAIChatCompletionService)));

        this._core.AddAttribute(AIServiceExtensions.ModelIdKey, modelId);
        this._core.AddAttribute(OpenAIClientCore.OrganizationKey, organization);
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
        this._core = new(modelId, openAIClient, loggerFactory?.CreateLogger(typeof(OpenAIChatCompletionService)));

        this._core.AddAttribute(AIServiceExtensions.ModelIdKey, modelId);
    }


    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Attributes => this.Attributes;


    /// <inheritdoc/>
    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chat, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        => this._core.GetChatMessageContentsAsync(chat, executionSettings, kernel, cancellationToken);


    /// <inheritdoc/>
    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        => this._core.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);


    /// <inheritdoc/>
    public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        => this._core.GetChatAsTextContentsAsync(prompt, executionSettings, kernel, cancellationToken);


    /// <inheritdoc/>
    public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        => this._core.GetChatAsTextStreamingContentsAsync(prompt, executionSettings, kernel, cancellationToken);
}