// Copyright (c) Microsoft.All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Microsoft.SemanticKernel.Memory;
/// <summary>
/// Implementation of <see cref="ISemanticTextMemory"/>. Provides methods to save, retrieve, and search for text information
/// in a semantic memory store.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SemanticTextMemory : ISemanticTextMemory
{
    private readonly ITextEmbeddingGenerationService? _textEmbeddingsService;
    private readonly IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;
    private readonly IMemoryStore _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticTextMemory"/> class.
    /// </summary>
    /// <param name="storage">The memory store to use for storing and retrieving data.</param>
    /// <param name="embeddingGenerator">The text embedding generator to use for generating embeddings.</param>
    [Obsolete("Use the constructor with IEmbeddingGenerator instead.")]
    public SemanticTextMemory(
        IMemoryStore storage,
        ITextEmbeddingGenerationService embeddingGenerator)
    {
        _textEmbeddingsService = embeddingGenerator;
        _storage = storage;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticTextMemory"/> class.
    /// </summary>
    /// <param name="storage">The memory store to use for storing and retrieving data.</param>
    /// <param name="embeddingGenerator">The text embedding generator to use for generating embeddings.</param>
    public SemanticTextMemory(
        IMemoryStore storage,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
        _storage = storage;
    }

    /// <inheritdoc/>
    public async Task<string> SaveInformationAsync(
        string collection,
        string text,
        string id,
        string? description = null,
        string? additionalMetadata = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        ReadOnlyMemory<float> embedding = await GenerateEmbeddingAsync(text, kernel, cancellationToken).
            ConfigureAwait(false);
        MemoryRecord data = MemoryRecord.LocalRecord(
            id: id,
            text: text,
            description: description,
            additionalMetadata: additionalMetadata,
            embedding: embedding);

        if (!(await _storage.DoesCollectionExistAsync(collection, cancellationToken).
                ConfigureAwait(false)))
        {
            await _storage.CreateCollectionAsync(collection, cancellationToken).
                ConfigureAwait(false);
        }

        return await _storage.UpsertAsync(collection, data, cancellationToken).
            ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> SaveReferenceAsync(
        string collection,
        string text,
        string externalId,
        string externalSourceName,
        string? description = null,
        string? additionalMetadata = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var embedding = await GenerateEmbeddingAsync(text, kernel, cancellationToken).
            ConfigureAwait(false);
        var data = MemoryRecord.ReferenceRecord(externalId: externalId, sourceName: externalSourceName, description: description,
            additionalMetadata: additionalMetadata, embedding: embedding);

        if (!(await _storage.DoesCollectionExistAsync(collection, cancellationToken).
                ConfigureAwait(false)))
        {
            await _storage.CreateCollectionAsync(collection, cancellationToken).
                ConfigureAwait(false);
        }

        return await _storage.UpsertAsync(collection, data, cancellationToken).
            ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<MemoryQueryResult?> GetAsync(
        string collection,
        string key,
        bool withEmbedding = false,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        MemoryRecord? record = await _storage.GetAsync(collection, key, withEmbedding, cancellationToken).
            ConfigureAwait(false);

        if (record is null) { return null; }

        return MemoryQueryResult.FromMemoryRecord(record, 1);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(
        string collection,
        string key,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        await _storage.RemoveAsync(collection, key, cancellationToken).
            ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<MemoryQueryResult> SearchAsync(
        string collection,
        string query,
        int limit = 1,
        double minRelevanceScore = 0.0,
        bool withEmbeddings = false,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await GenerateEmbeddingAsync(query, kernel, cancellationToken).
            ConfigureAwait(false);

        if ((await _storage.DoesCollectionExistAsync(collection, cancellationToken).
                ConfigureAwait(false)))
        {
            IAsyncEnumerable<(MemoryRecord, double)> results = _storage.GetNearestMatchesAsync(
                collectionName: collection,
                embedding: queryEmbedding,
                limit: limit,
                minRelevanceScore: minRelevanceScore,
                withEmbeddings: withEmbeddings,
                cancellationToken: cancellationToken);

            await foreach ((MemoryRecord, double) result in results.WithCancellation(cancellationToken).
                               ConfigureAwait(false))
            {
                yield return MemoryQueryResult.FromMemoryRecord(result.Item1, result.Item2);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<IList<string>> GetCollectionsAsync(Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        return await _storage.GetCollectionsAsync(cancellationToken).
            ToListAsync(cancellationToken).
            ConfigureAwait(false);
    }

    private async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, Kernel? kernel, CancellationToken cancellationToken)
    {
        return (_textEmbeddingsService is not null)
                    ? await _textEmbeddingsService.GenerateEmbeddingAsync(text, kernel, cancellationToken).ConfigureAwait(false)
                    : (await _embeddingGenerator!.GenerateAsync(text, cancellationToken: cancellationToken).ConfigureAwait(false)).Vector;
    }
}
