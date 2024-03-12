// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Embeddings;
using Extensions.Logging;
using Services;


/// <summary>
/// OpenAI text embedding service.
/// </summary>
public sealed class OpenAITextEmbeddingGenerationService : ITextEmbeddingGenerationService
{

    private readonly OpenAIClientCore _core;


    /// <summary>
    /// Create an instance of the OpenAI text embedding connector
    /// </summary>
    /// <param name="modelId">Model name</param>
    /// <param name="apiKey">OpenAI API Key</param>
    /// <param name="organization">OpenAI Organization Id (usually optional)</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public OpenAITextEmbeddingGenerationService(
        string modelId,
        string apiKey,
        string? organization = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        _core = new OpenAIClientCore(modelId, apiKey, organization, httpClient,
            loggerFactory?.CreateLogger(typeof(OpenAITextEmbeddingGenerationService)));

        _core.AddAttribute(AIServiceExtensions.ModelIdKey, modelId);
    }


    /// <summary>
    /// Create an instance of the OpenAI text embedding connector
    /// </summary>
    /// <param name="modelId">Model name</param>
    /// <param name="openAIClient">Custom <see cref="OpenAIClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public OpenAITextEmbeddingGenerationService(
        string modelId,
        OpenAIClient openAIClient,
        ILoggerFactory? loggerFactory = null)
    {
        _core = new OpenAIClientCore(modelId, openAIClient, loggerFactory?.CreateLogger(typeof(OpenAITextEmbeddingGenerationService)));
        _core.AddAttribute(AIServiceExtensions.ModelIdKey, modelId);
    }


    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Attributes => _core.Attributes;


    /// <inheritdoc/>
    public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IList<string> data,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        _core.LogActionDetails();

        return _core.GetEmbeddingsAsync(data, kernel, cancellationToken);
    }

}
