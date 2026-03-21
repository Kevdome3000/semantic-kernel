// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.VectorData;

namespace Microsoft.SemanticKernel.Connectors.CosmosNoSql;

/// <summary>
/// Class for accessing the list of collections in a Azure CosmosDB NoSQL vector store.
/// </summary>
/// <remarks>
/// This class can be used with collections of any schema type, but requires you to provide schema information when getting a collection.
/// </remarks>
public sealed class CosmosNoSqlVectorStore : VectorStore
{
    /// <summary>Metadata about vector store.</summary>
    private readonly VectorStoreMetadata _metadata;

    /// <summary><see cref="Database"/> that can be used to manage the collections in Azure CosmosDB NoSQL.</summary>
    private readonly Database _database;

    private readonly ClientWrapper _clientWrapper;

    /// <summary>A general purpose definition that can be used to construct a collection when needing to proxy schema agnostic operations.</summary>
    private static readonly VectorStoreCollectionDefinition s_generalPurposeDefinition = new() { Properties = [new VectorStoreKeyProperty("Key", typeof(string))] };

    private readonly JsonSerializerOptions? _jsonSerializerOptions;
    private readonly IEmbeddingGenerator? _embeddingGenerator;


    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosNoSqlVectorStore"/> class.
    /// </summary>
    /// <param name="database"><see cref="Database"/> that can be used to manage the collections in Azure CosmosDB NoSQL.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    [RequiresUnreferencedCode("The Cosmos NoSQL provider is currently incompatible with trimming.")]
    [RequiresDynamicCode("The Cosmos NoSQL provider is currently incompatible with NativeAOT.")]
    public CosmosNoSqlVectorStore(Database database, CosmosNoSqlVectorStoreOptions? options = null)
        : this(new(database.Client, ownsClient: false), _ => database, options)
    {
        Verify.NotNull(database);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosNoSqlVectorStore"/> class.
    /// </summary>
    /// <param name="connectionString">Connection string required to connect to Azure CosmosDB NoSQL.</param>
    /// <param name="databaseName">Database name for Azure CosmosDB NoSQL.</param>
    /// <param name="clientOptions">Optional configuration options for <see cref="CosmosClient"/>.</param>
    /// <param name="storeOptions">Optional configuration options for <see cref="VectorStore"/>.</param>
    public CosmosNoSqlVectorStore(
        string connectionString,
        string databaseName,
        CosmosClientOptions? clientOptions = null,
        CosmosNoSqlVectorStoreOptions? storeOptions = null)
        : this(new ClientWrapper(new CosmosClient(connectionString, clientOptions), true), client => client.GetDatabase(databaseName), storeOptions)
    {
        Verify.NotNullOrWhiteSpace(connectionString);
        Verify.NotNullOrWhiteSpace(databaseName);
    }


    private CosmosNoSqlVectorStore(
        ClientWrapper clientWrapper,
        Func<CosmosClient, Database> databaseProvider,
        CosmosNoSqlVectorStoreOptions? options)
    {
        try
        {
            _database = databaseProvider(clientWrapper.Client);
            _embeddingGenerator = options?.EmbeddingGenerator;
            _jsonSerializerOptions = options?.JsonSerializerOptions;

            _metadata = new()
            {
                VectorStoreSystemName = CosmosNoSqlConstants.VectorStoreSystemName,
                VectorStoreName = _database.Id
            };
        }
        catch (Exception)
        {
            // Something went wrong, we dispose the client and don't store a reference.
            clientWrapper.Dispose();

            throw;
        }

        _clientWrapper = clientWrapper;
    }


    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        _clientWrapper.Dispose();
        base.Dispose(disposing);
    }


#pragma warning disable IDE0090 // Use 'new(...)'
    /// <inheritdoc />
    [RequiresUnreferencedCode("The Cosmos NoSQL provider is currently incompatible with trimming.")]
    [RequiresDynamicCode("The Cosmos NoSQL provider is currently incompatible with NativeAOT.")]
#if NET
    public override CosmosNoSqlCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name, VectorStoreCollectionDefinition? definition = null)
#else
    public override VectorStoreCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name, VectorStoreCollectionDefinition? definition = null)
#endif
        => typeof(TRecord) == typeof(Dictionary<string, object?>)
            ? throw new ArgumentException(VectorDataStrings.GetCollectionWithDictionaryNotSupported)
            : new CosmosNoSqlCollection<TKey, TRecord>(
                _clientWrapper.Share(),
                _ => _database,
                name,
                new()
                {
                    Definition = definition,
                    JsonSerializerOptions = _jsonSerializerOptions,
                    EmbeddingGenerator = _embeddingGenerator
                });


    /// <inheritdoc />
    [RequiresUnreferencedCode("The Cosmos NoSQL provider is currently incompatible with trimming.")]
    [RequiresDynamicCode("The Cosmos NoSQL provider is currently incompatible with NativeAOT.")]
#if NET
    public override CosmosNoSqlDynamicCollection GetDynamicCollection(string name, VectorStoreCollectionDefinition definition)
#else
    public override VectorStoreCollection<object, Dictionary<string, object?>> GetDynamicCollection(string name, VectorStoreCollectionDefinition definition)
#endif
        => new CosmosNoSqlDynamicCollection(
            _clientWrapper.Share(),
            _ => _database,
            name,
            new()
            {
                Definition = definition,
                JsonSerializerOptions = _jsonSerializerOptions,
                EmbeddingGenerator = _embeddingGenerator
            }
        );
#pragma warning restore IDE0090


    /// <inheritdoc />
    public override async IAsyncEnumerable<string> ListCollectionNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string Query = "SELECT VALUE(c.id) FROM c";

        const string OperationName = "ListCollectionNamesAsync";
        using var feedIterator = VectorStoreErrorHandler.RunOperation<FeedIterator<string>, CosmosException>(
            _metadata,
            OperationName,
            () => _database.GetContainerQueryIterator<string>(Query));
        using var errorHandlingFeedIterator = new ErrorHandlingFeedIterator<string>(feedIterator, _metadata, OperationName);

        while (feedIterator.HasMoreResults)
        {
            var next = await feedIterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);

            foreach (var containerName in next.Resource)
            {
                yield return containerName;
            }
        }
    }


    /// <inheritdoc />
    public override Task<bool> CollectionExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var collection = GetDynamicCollection(name, s_generalPurposeDefinition);
        return collection.CollectionExistsAsync(cancellationToken);
    }


    /// <inheritdoc />
    public override Task EnsureCollectionDeletedAsync(string name, CancellationToken cancellationToken = default)
    {
        var collection = GetDynamicCollection(name, s_generalPurposeDefinition);
        return collection.EnsureCollectionDeletedAsync(cancellationToken);
    }


    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        Verify.NotNull(serviceType);

        return
            serviceKey is not null
                ? null
                : serviceType == typeof(VectorStoreMetadata)
                    ? _metadata
                    : serviceType == typeof(Database)
                        ? _database
                        : serviceType.IsInstanceOfType(this)
                            ? this
                            : null;
    }
}
