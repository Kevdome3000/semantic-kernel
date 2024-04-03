// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Google;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Core;
using Embeddings;
using Extensions.Logging;
using Http;
using Services;


/// <summary>
/// Represents a service for generating text embeddings using the Google AI Gemini API.
/// </summary>
public sealed class GoogleAITextEmbeddingGenerationService : ITextEmbeddingGenerationService
{

    private readonly Dictionary<string, object?> _attributesInternal = new();

    private readonly GoogleAIEmbeddingClient _embeddingClient;


    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleAITextEmbeddingGenerationService"/> class.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <param name="httpClient">The optional HTTP client.</param>
    /// <param name="loggerFactory">Optional logger factory to be used for logging.</param>
    public GoogleAITextEmbeddingGenerationService(
        string modelId,
        string apiKey,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNullOrWhiteSpace(modelId);
        Verify.NotNullOrWhiteSpace(apiKey);

        this._embeddingClient = new GoogleAIEmbeddingClient(
#pragma warning disable CA2000
            httpClient: HttpClientProvider.GetHttpClient(httpClient),
#pragma warning restore CA2000
            modelId: modelId,
            apiKey: apiKey,
            logger: loggerFactory?.CreateLogger(typeof(GoogleAITextEmbeddingGenerationService)));

        this._attributesInternal.Add(AIServiceExtensions.ModelIdKey, modelId);
    }


    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Attributes => this._attributesInternal;


    /// <inheritdoc />
    public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IList<string> data,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        return this._embeddingClient.GenerateEmbeddingsAsync(data, cancellationToken);
    }

}
