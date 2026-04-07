// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.VectorData;

namespace Microsoft.SemanticKernel.Connectors.CosmosNoSql;

internal class ErrorHandlingFeedIterator<T> : FeedIterator<T>
{
    private readonly FeedIterator<T> _internalFeedIterator;
    private readonly string _operationName;
    private readonly VectorStoreCollectionMetadata _collectionMetadata;


    public ErrorHandlingFeedIterator(
        FeedIterator<T> internalFeedIterator,
        VectorStoreCollectionMetadata collectionMetadata,
        string operationName)
    {
        _internalFeedIterator = internalFeedIterator;
        _operationName = operationName;
        _collectionMetadata = collectionMetadata;
    }


    public ErrorHandlingFeedIterator(
        FeedIterator<T> internalFeedIterator,
        VectorStoreMetadata metadata,
        string operationName)
    {
        _internalFeedIterator = internalFeedIterator;
        _operationName = operationName;
        _collectionMetadata = new VectorStoreCollectionMetadata
        {
            CollectionName = null,
            VectorStoreName = metadata.VectorStoreName,
            VectorStoreSystemName = metadata.VectorStoreSystemName
        };
    }


    public override bool HasMoreResults => _internalFeedIterator.HasMoreResults;


    public override Task<FeedResponse<T>> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        return VectorStoreErrorHandler.RunOperationAsync<FeedResponse<T>, CosmosException>(
            _collectionMetadata,
            _operationName,
            () => _internalFeedIterator.ReadNextAsync(cancellationToken));
    }
}
