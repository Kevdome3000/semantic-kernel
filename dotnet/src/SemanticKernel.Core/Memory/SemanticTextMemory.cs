// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Memory;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Embeddings;


/// <summary>
/// Implementation of <see cref="ISemanticTextMemory"/>. Provides methods to save, retrieve, and search for text information
/// in a semantic memory store.
/// </summary>
public sealed class SemanticTextMemory : ISemanticTextMemory
{

    private readonly ITextEmbeddingGenerationService _embeddingGenerator;

    private readonly IMemoryStore _storage;


    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticTextMemory"/> class.
    /// </summary>
    /// <param name="storage">The memory store to use for storing and retrieving data.</param>
    /// <param name="embeddingGenerator">The text embedding generator to use for generating embeddings.</param>
    public SemanticTextMemory(
        IMemoryStore storage,
        ITextEmbeddingGenerationService embeddingGenerator)
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
        var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(text, kernel, cancellationToken).
            ConfigureAwait(false);

        MemoryRecord data = MemoryRecord.LocalRecord(
            id, text, description, additionalMetadata: additionalMetadata,
            embedding: embedding);

        if (!await _storage.DoesCollectionExistAsync(collection, cancellationToken).
                ConfigureAwait(false))
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
        var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(text, kernel, cancellationToken).
            ConfigureAwait(false);

        var data = MemoryRecord.ReferenceRecord(externalId, externalSourceName, description,
            additionalMetadata: additionalMetadata, embedding: embedding);

        if (!await _storage.DoesCollectionExistAsync(collection, cancellationToken).
                ConfigureAwait(false))
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

        if (record == null)
        {
            return null;
        }

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
        ReadOnlyMemory<float> queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(query, kernel, cancellationToken).
            ConfigureAwait(false);

        IAsyncEnumerable<(MemoryRecord, double)> results = _storage.GetNearestMatchesAsync(
            collection,
            queryEmbedding,
            limit,
            minRelevanceScore,
            withEmbeddings,
            cancellationToken);

        await foreach ((MemoryRecord, double) result in results.WithCancellation(cancellationToken).
                           ConfigureAwait(false))
        {
            yield return MemoryQueryResult.FromMemoryRecord(result.Item1, result.Item2);
        }
    }


    /// <inheritdoc/>
    public async Task<IList<string>> GetCollectionsAsync(Kernel? kernel = null, CancellationToken cancellationToken = default) => await _storage.GetCollectionsAsync(cancellationToken).
        ToListAsync(cancellationToken).
        ConfigureAwait(false);

}
