// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;

namespace Microsoft.SemanticKernel.Connectors.MongoDB;

/// <summary>
/// A decorator for <see cref="IAsyncCursor{T}"/> that handles errors on move next.
/// </summary>
/// <typeparam name="T">The type that the cursor returns.</typeparam>
internal class ErrorHandlingAsyncCursor<T> : IAsyncCursor<T>
{
    private readonly IAsyncCursor<T> _cursor;
    private readonly string _operationName;
    private readonly VectorStoreCollectionMetadata _collectionMetadata;


    public ErrorHandlingAsyncCursor(IAsyncCursor<T> cursor, VectorStoreCollectionMetadata collectionMetadata, string operationName)
    {
        _cursor = cursor;
        _operationName = operationName;
        _collectionMetadata = collectionMetadata;
    }


    public ErrorHandlingAsyncCursor(IAsyncCursor<T> cursor, VectorStoreMetadata metadata, string operationName)
    {
        _cursor = cursor;
        _operationName = operationName;
        _collectionMetadata = new VectorStoreCollectionMetadata
        {
            CollectionName = null,
            VectorStoreName = metadata.VectorStoreName,
            VectorStoreSystemName = metadata.VectorStoreSystemName
        };
    }


    public IEnumerable<T> Current => _cursor.Current;


    public void Dispose()
    {
        _cursor.Dispose();
    }


    public bool MoveNext(CancellationToken cancellationToken = default)
    {
        return VectorStoreErrorHandler.RunOperation<bool, MongoException>(
            _collectionMetadata,
            _operationName,
            () => _cursor.MoveNext(cancellationToken));
    }


    public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        return VectorStoreErrorHandler.RunOperationAsync<bool, MongoException>(
            _collectionMetadata,
            _operationName,
            () => _cursor.MoveNextAsync(cancellationToken));
    }
}
