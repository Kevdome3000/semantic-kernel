﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Logging;
using Services;
using TextToAudio;


/// <summary>
/// OpenAI text-to-audio service.
/// </summary>
public sealed class OpenAITextToAudioService : ITextToAudioService
{

    /// <summary>
    /// OpenAI text-to-audio client for HTTP operations.
    /// </summary>
    private readonly OpenAITextToAudioClient _client;

    /// <summary>
    /// Gets the attribute name used to store the organization in the <see cref="IAIService.Attributes"/> dictionary.
    /// </summary>
    public static string OrganizationKey => "Organization";

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Attributes => _client.Attributes;


    /// <summary>
    /// Creates an instance of the <see cref="OpenAITextToAudioService"/> with API key auth.
    /// </summary>
    /// <param name="modelId">Model name</param>
    /// <param name="apiKey">OpenAI API Key</param>
    /// <param name="organization">OpenAI Organization Id (usually optional)</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public OpenAITextToAudioService(
        string modelId,
        string apiKey,
        string? organization = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        _client = new OpenAITextToAudioClient(modelId, apiKey, organization, httpClient,
            loggerFactory?.CreateLogger(typeof(OpenAITextToAudioService)));

        _client.AddAttribute(AIServiceExtensions.ModelIdKey, modelId);
        _client.AddAttribute(OrganizationKey, organization);
    }


    /// <inheritdoc/>
    public Task<IReadOnlyList<AudioContent>> GetAudioContentsAsync(
        string text,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
        => _client.GetAudioContentsAsync(text, executionSettings, cancellationToken);

}
