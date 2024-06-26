﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AudioToText;
using Azure.AI.OpenAI;
using Azure.Core;
using Extensions.Logging;
using Services;


/// <summary>
/// Azure OpenAI audio-to-text service.
/// </summary>
public sealed class AzureOpenAIAudioToTextService : IAudioToTextService
{

    /// <summary>Core implementation shared by Azure OpenAI services.</summary>
    private readonly AzureOpenAIClientCore _core;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Attributes => _core.Attributes;


    /// <summary>
    /// Creates an instance of the <see cref="AzureOpenAIAudioToTextService"/> with API key auth.
    /// </summary>
    /// <param name="deploymentName">Azure OpenAI deployment name, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="endpoint">Azure OpenAI deployment URL, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="apiKey">Azure OpenAI API key, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="modelId">Azure OpenAI model id, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public AzureOpenAIAudioToTextService(
        string deploymentName,
        string endpoint,
        string apiKey,
        string? modelId = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        _core = new AzureOpenAIClientCore(deploymentName, endpoint, apiKey, httpClient,
            loggerFactory?.CreateLogger(typeof(AzureOpenAIAudioToTextService)));

        _core.AddAttribute(AIServiceExtensions.ModelIdKey, modelId);
    }


    /// <summary>
    /// Creates an instance of the <see cref="AzureOpenAIAudioToTextService"/> with AAD auth.
    /// </summary>
    /// <param name="deploymentName">Azure OpenAI deployment name, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="endpoint">Azure OpenAI deployment URL, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="credentials">Token credentials, e.g. DefaultAzureCredential, ManagedIdentityCredential, EnvironmentCredential, etc.</param>
    /// <param name="modelId">Azure OpenAI model id, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public AzureOpenAIAudioToTextService(
        string deploymentName,
        string endpoint,
        TokenCredential credentials,
        string? modelId = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        _core = new AzureOpenAIClientCore(deploymentName, endpoint, credentials, httpClient,
            loggerFactory?.CreateLogger(typeof(AzureOpenAIAudioToTextService)));

        _core.AddAttribute(AIServiceExtensions.ModelIdKey, modelId);
    }


    /// <summary>
    /// Creates an instance of the <see cref="AzureOpenAIAudioToTextService"/> using the specified <see cref="OpenAIClient"/>.
    /// </summary>
    /// <param name="deploymentName">Azure OpenAI deployment name, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="openAIClient">Custom <see cref="OpenAIClient"/>.</param>
    /// <param name="modelId">Azure OpenAI model id, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public AzureOpenAIAudioToTextService(
        string deploymentName,
        OpenAIClient openAIClient,
        string? modelId = null,
        ILoggerFactory? loggerFactory = null)
    {
        _core = new AzureOpenAIClientCore(deploymentName, openAIClient, loggerFactory?.CreateLogger(typeof(AzureOpenAIAudioToTextService)));
        _core.AddAttribute(AIServiceExtensions.ModelIdKey, modelId);
    }


    /// <inheritdoc/>
    public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        AudioContent content,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
        => _core.GetTextContentFromAudioAsync(content, executionSettings, cancellationToken);

}
