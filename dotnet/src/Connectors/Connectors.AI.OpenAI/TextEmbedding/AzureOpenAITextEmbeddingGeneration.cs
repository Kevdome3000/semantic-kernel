﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using AzureSdk;
using Extensions.Logging;
using SemanticKernel.AI.Embeddings;


/// <summary>
/// Azure OpenAI text embedding service.
/// </summary>
public sealed class AzureOpenAITextEmbeddingGeneration : AzureOpenAIClientBase, ITextEmbeddingGeneration
{
    /// <summary>
    /// Creates a new <see cref="AzureOpenAITextEmbeddingGeneration"/> client instance using API Key auth.
    /// </summary>
    /// <param name="modelId">Azure OpenAI model ID or deployment name, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="endpoint">Azure OpenAI deployment URL, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="apiKey">Azure OpenAI API key, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public AzureOpenAITextEmbeddingGeneration(
        string modelId,
        string endpoint,
        string apiKey,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null) : base(modelId, endpoint, apiKey, httpClient, loggerFactory)
    {
    }


    /// <summary>
    /// Creates a new <see cref="AzureOpenAITextEmbeddingGeneration"/> client instance supporting AAD auth.
    /// </summary>
    /// <param name="modelId">Azure OpenAI model ID or deployment name, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="endpoint">Azure OpenAI deployment URL, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="credential">Token credentials, e.g. DefaultAzureCredential, ManagedIdentityCredential, EnvironmentCredential, etc.</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public AzureOpenAITextEmbeddingGeneration(
        string modelId,
        string endpoint,
        TokenCredential credential,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null) : base(modelId, endpoint, credential, httpClient, loggerFactory)
    {
    }


    /// <summary>
    /// Generates an embedding from the given <paramref name="data"/>.
    /// </summary>
    /// <param name="data">List of strings to generate embeddings for</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>List of embeddings</returns>
    public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IList<string> data,
        CancellationToken cancellationToken = default)
    {
        this.LogActionDetails();
        return this.InternalGetEmbeddingsAsync(data, cancellationToken);
    }
}