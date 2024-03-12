// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Memory;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


/// <summary>
/// Implementation of <see cref="ISemanticTextMemory"/> that stores nothing.
/// </summary>
public sealed class NullMemory : ISemanticTextMemory
{

    private static readonly Task<string> s_emptyStringTask = Task.FromResult(string.Empty);

    /// <summary>
    /// Singleton instance
    /// </summary>
    public static NullMemory Instance { get; } = new();


    /// <inheritdoc/>
    public Task<string> SaveInformationAsync(
        string collection,
        string text,
        string id,
        string? description = null,
        string? additionalMetadata = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default) => s_emptyStringTask;


    /// <inheritdoc/>
    public Task<string> SaveReferenceAsync(
        string collection,
        string text,
        string externalId,
        string externalSourceName,
        string? description = null,
        string? additionalMetadata = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default) => s_emptyStringTask;


    /// <inheritdoc/>
    public Task<MemoryQueryResult?> GetAsync(
        string collection,
        string key,
        bool withEmbedding = false,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default) => Task.FromResult<MemoryQueryResult?>(null);


    /// <inheritdoc/>
    public Task RemoveAsync(
        string collection,
        string key,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default) => Task.CompletedTask;


    /// <inheritdoc/>
    public IAsyncEnumerable<MemoryQueryResult> SearchAsync(
        string collection,
        string query,
        int limit = 1,
        double minRelevanceScore = 0.0,
        bool withEmbeddings = false,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default) => AsyncEnumerable.Empty<MemoryQueryResult>();


    /// <inheritdoc/>
    public Task<IList<string>> GetCollectionsAsync(
        Kernel? kernel = null,
        CancellationToken cancellationToken = default) => Task.FromResult<IList<string>>(new List<string>());


    private NullMemory()
    {
    }

}
