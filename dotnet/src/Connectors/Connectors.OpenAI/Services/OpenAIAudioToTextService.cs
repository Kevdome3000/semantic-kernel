// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AudioToText;
using OpenAI;

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

/// <summary>
/// OpenAI text-to-audio service.
/// </summary>
[Experimental("SKEXP0010")]
public sealed class OpenAIAudioToTextService : IAudioToTextService
{
    /// <summary>
    /// OpenAI text-to-audio client for HTTP operations.
    /// </summary>
    private readonly ClientCore _client;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Attributes => _client.Attributes;


    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIAudioToTextService"/> class.
    /// </summary>
    /// <param name="modelId">Model name</param>
    /// <param name="apiKey">OpenAI API Key</param>
    /// <param name="organization">OpenAI Organization Id (usually optional)</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public OpenAIAudioToTextService(
        string modelId,
        string apiKey,
        string? organization = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNullOrWhiteSpace(modelId);
        _client = new ClientCore(modelId,
            apiKey,
            organization,
            null,
            httpClient,
            loggerFactory?.CreateLogger(typeof(OpenAIAudioToTextService)));
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIAudioToTextService"/> class.
    /// </summary>
    /// <param name="modelId">Model name</param>
    /// <param name="openAIClient">Custom <see cref="OpenAIClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public OpenAIAudioToTextService(
        string modelId,
        OpenAIClient openAIClient,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNullOrWhiteSpace(modelId);
        _client = new ClientCore(modelId, openAIClient, loggerFactory?.CreateLogger(typeof(OpenAITextToAudioService)));
    }


    /// <inheritdoc/>
    public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        AudioContent content,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        return _client.GetTextFromAudioContentsAsync(_client.ModelId,
            content,
            executionSettings,
            cancellationToken);
    }
}
