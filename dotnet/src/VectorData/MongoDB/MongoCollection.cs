// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using MEVD = Microsoft.Extensions.VectorData;

namespace Microsoft.SemanticKernel.Connectors.MongoDB;

/// <summary>
/// Service for storing and retrieving vector records, that uses MongoDB as the underlying storage.
/// </summary>
/// <typeparam name="TKey">The data type of the record key. Must be <see cref="string"/>.</typeparam>
/// <typeparam name="TRecord">The data model to use for adding, updating and retrieving data from storage.</typeparam>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class MongoCollection<TKey, TRecord> : VectorStoreCollection<TKey, TRecord>, IKeywordHybridSearchable<TRecord>
    where TKey : notnull
    where TRecord : class
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    /// <summary>Metadata about vector store record collection.</summary>
    private readonly VectorStoreCollectionMetadata _collectionMetadata;

    /// <summary>Property name to be used for search similarity score value.</summary>
    private const string ScorePropertyName = "similarityScore";

    /// <summary>Property name to be used for search document value.</summary>
    private const string DocumentPropertyName = "document";

    /// <summary>The default options for vector search.</summary>
    private static readonly MEVD.VectorSearchOptions<TRecord> s_defaultVectorSearchOptions = new();

    /// <summary>The default options for hybrid vector search.</summary>
    private static readonly HybridSearchOptions<TRecord> s_defaultKeywordVectorizedHybridSearchOptions = new();

    /// <summary><see cref="IMongoDatabase"/> that can be used to manage the collections in MongoDB.</summary>
    private readonly IMongoDatabase _mongoDatabase;

    /// <summary>MongoDB collection to perform record operations.</summary>
    private readonly IMongoCollection<BsonDocument> _mongoCollection;

    /// <summary>Interface for mapping between a storage model, and the consumer record data model.</summary>
    private readonly IMongoMapper<TRecord> _mapper;

    /// <summary>The model for this collection.</summary>
    private readonly CollectionModel _model;

    /// <inheritdoc />
    public override string Name { get; }

    /// <summary>Vector index name to use.</summary>
    private readonly string _vectorIndexName;

    /// <summary>Full text search index name to use.</summary>
    private readonly string _fullTextSearchIndexName;

    /// <summary>Number of max retries for vector collection operation.</summary>
    private readonly int _maxRetries;

    /// <summary>Delay in milliseconds between retries for vector collection operation.</summary>
    private readonly int _delayInMilliseconds;

    /// <summary>Number of nearest neighbors to use during the vector search.</summary>
    private readonly int? _numCandidates;

    /// <summary><see cref="BsonSerializationInfo"/> to use for serializing key values.</summary>
    private readonly BsonSerializationInfo? _keySerializationInfo;

    /// <summary>Types of keys permitted.</summary>
    private static readonly Type[] s_validKeyTypes = [typeof(string), typeof(Guid), typeof(ObjectId), typeof(int), typeof(long)];


    /// <summary>
    /// Initializes a new instance of the <see cref="MongoCollection{TKey, TRecord}"/> class.
    /// </summary>
    /// <param name="mongoDatabase"><see cref="IMongoDatabase"/> that can be used to manage the collections in MongoDB.</param>
    /// <param name="name">The name of the collection that this <see cref="MongoCollection{TKey, TRecord}"/> will access.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    [RequiresDynamicCode("This constructor is incompatible with NativeAOT. For dynamic mapping via Dictionary<string, object?>, instantiate MongoDynamicCollection instead.")]
    [RequiresUnreferencedCode("This constructor is incompatible with trimming. For dynamic mapping via Dictionary<string, object?>, instantiate MongoDynamicCollection instead.")]
    public MongoCollection(
        IMongoDatabase mongoDatabase,
        string name,
        MongoCollectionOptions? options = default)
        : this(
            mongoDatabase,
            name,
            static options => typeof(TRecord) == typeof(Dictionary<string, object?>)
                ? throw new NotSupportedException(VectorDataStrings.NonDynamicCollectionWithDictionaryNotSupported(typeof(MongoDynamicCollection)))
                : new MongoModelBuilder().Build(typeof(TRecord),
                    typeof(TKey),
                    options.Definition,
                    options.EmbeddingGenerator),
            options)
    {
    }


    internal MongoCollection(
        IMongoDatabase mongoDatabase,
        string name,
        Func<MongoCollectionOptions, CollectionModel> modelFactory,
        MongoCollectionOptions? options)
    {
        // Verify.
        Verify.NotNull(mongoDatabase);
        Verify.NotNullOrWhiteSpace(name);

        if (!s_validKeyTypes.Contains(typeof(TKey)) && typeof(TKey) != typeof(object))
        {
            throw new NotSupportedException("Only ObjectID, string, Guid, int and long keys are supported.");
        }

        options ??= MongoCollectionOptions.Default;

        // Assign.
        _mongoDatabase = mongoDatabase;
        _mongoCollection = mongoDatabase.GetCollection<BsonDocument>(name);
        Name = name;
        _model = modelFactory(options);

        _vectorIndexName = options.VectorIndexName;
        _fullTextSearchIndexName = options.FullTextSearchIndexName;
        _maxRetries = options.MaxRetries;
        _delayInMilliseconds = options.DelayInMilliseconds;
        _numCandidates = options.NumCandidates;

        _mapper = typeof(TRecord) == typeof(Dictionary<string, object?>)
            ? (new MongoDynamicMapper(_model) as IMongoMapper<TRecord>)!
            : new MongoMapper<TRecord>(_model);

        _collectionMetadata = new()
        {
            VectorStoreSystemName = MongoConstants.VectorStoreSystemName,
            VectorStoreName = mongoDatabase.DatabaseNamespace?.DatabaseName,
            CollectionName = name
        };

        // Cache the key serialization info if possible
        _keySerializationInfo = typeof(TKey) == typeof(object)
            ? null
            : GetKeySerializationInfo();
    }


    /// <inheritdoc />
    public override Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        return this.RunOperationAsync("ListCollectionNames", () => InternalCollectionExistsAsync(cancellationToken));
    }


    /// <inheritdoc />
    public override async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        // The IMongoDatabase.CreateCollectionAsync "Creates a new collection if not already available".
        // So for EnsureCollectionExistsAsync, we don't perform an additional check.
        await this.RunOperationAsync("CreateCollection",
                () => _mongoDatabase.CreateCollectionAsync(Name, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        await this.RunOperationWithRetryAsync(
                "CreateIndexes",
                _maxRetries,
                _delayInMilliseconds,
                () => CreateIndexesAsync(Name, cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);
    }


    /// <inheritdoc />
    public override async Task DeleteAsync(TKey key, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(key);

        await this.RunOperationAsync("DeleteOne", () => _mongoCollection.DeleteOneAsync(GetFilterById(key), cancellationToken))
            .ConfigureAwait(false);
    }


    /// <inheritdoc />
    public override async Task DeleteAsync(IEnumerable<TKey> keys, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(keys);

        await this.RunOperationAsync("DeleteMany", () => _mongoCollection.DeleteManyAsync(GetFilterByIds(keys), cancellationToken))
            .ConfigureAwait(false);
    }


    /// <inheritdoc />
    public override Task EnsureCollectionDeletedAsync(CancellationToken cancellationToken = default)
    {
        return this.RunOperationAsync("DropCollection", () => _mongoDatabase.DropCollectionAsync(Name, cancellationToken));
    }


    /// <inheritdoc />
    public override async Task<TRecord?> GetAsync(TKey key, RecordRetrievalOptions? options = null, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(key);

        var includeVectors = options?.IncludeVectors ?? false;

        if (includeVectors && _model.EmbeddingGenerationRequired)
        {
            throw new NotSupportedException(VectorDataStrings.IncludeVectorsNotSupportedWithEmbeddingGeneration);
        }

        using var cursor = await this
            .FindAsync(GetFilterById(key),
                top: 1,
                skip: null,
                includeVectors,
                sortDefinition: null,
                cancellationToken)
            .ConfigureAwait(false);

        var record = await cursor.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (record is null)
        {
            return default;
        }

        return _mapper.MapFromStorageToDataModel(record, includeVectors);
    }


    /// <inheritdoc />
    public override async IAsyncEnumerable<TRecord> GetAsync(
        IEnumerable<TKey> keys,
        RecordRetrievalOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Verify.NotNull(keys);

        var includeVectors = options?.IncludeVectors ?? false;

        if (includeVectors && _model.EmbeddingGenerationRequired)
        {
            throw new NotSupportedException(VectorDataStrings.IncludeVectorsNotSupportedWithEmbeddingGeneration);
        }

        using var cursor = await this
            .FindAsync(GetFilterByIds(keys),
                top: null,
                skip: null,
                includeVectors,
                sortDefinition: null,
                cancellationToken)
            .ConfigureAwait(false);

        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var record in cursor.Current)
            {
                if (record is not null)
                {
                    yield return _mapper.MapFromStorageToDataModel(record, includeVectors);
                }
            }
        }
    }


    /// <inheritdoc />
    public override async Task UpsertAsync(TRecord record, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(record);

        (_, var generatedEmbeddings) = await ProcessEmbeddingsAsync(_model, [record], cancellationToken).ConfigureAwait(false);

        await UpsertCoreAsync(record,
                0,
                generatedEmbeddings,
                cancellationToken)
            .ConfigureAwait(false);
    }


    /// <inheritdoc />
    public override async Task UpsertAsync(IEnumerable<TRecord> records, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(records);

        (records, var generatedEmbeddings) = await ProcessEmbeddingsAsync(_model, records, cancellationToken).ConfigureAwait(false);

        var i = 0;

        foreach (var record in records)
        {
            await UpsertCoreAsync(record,
                    i++,
                    generatedEmbeddings,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }


    private async Task UpsertCoreAsync(
        TRecord record,
        int recordIndex,
        IReadOnlyList<Embedding>?[]? generatedEmbeddings,
        CancellationToken cancellationToken = default)
    {
        const string OperationName = "ReplaceOne";

        // Handle auto-generated keys
        var keyProperty = _model.KeyProperty;

        if (keyProperty.IsAutoGenerated)
        {
            switch (keyProperty.Type)
            {
                case var t when t == typeof(Guid):
                    if (keyProperty.GetValue<Guid>(record) == Guid.Empty)
                    {
                        keyProperty.SetValue(record, Guid.NewGuid());
                    }
                    break;

                case var t when t == typeof(ObjectId):
                    if (keyProperty.GetValue<ObjectId>(record) == ObjectId.Empty)
                    {
                        keyProperty.SetValue(record, ObjectId.GenerateNewId());
                    }
                    break;

                default:
                    throw new UnreachableException();
            }
        }

        var replaceOptions = new ReplaceOptions { IsUpsert = true };
        var storageModel = _mapper.MapFromDataToStorageModel(record, recordIndex, generatedEmbeddings);

        var key = GetStorageKey(storageModel);

        await this.RunOperationAsync(OperationName,
                async () =>
                    await _mongoCollection
                        .ReplaceOneAsync(GetFilterById(key),
                            storageModel,
                            replaceOptions,
                            cancellationToken)
                        .ConfigureAwait(false))
            .ConfigureAwait(false);
    }


    private static TKey GetStorageKey(BsonDocument document)
    {
        return (TKey)BsonTypeMapper.MapToDotNetValue(document[MongoConstants.MongoReservedKeyPropertyName]);
    }


    private static async ValueTask<(IEnumerable<TRecord> records, IReadOnlyList<Embedding>?[]?)> ProcessEmbeddingsAsync(
        CollectionModel model,
        IEnumerable<TRecord> records,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TRecord>? recordsList = null;

        // If an embedding generator is defined, invoke it once per property for all records.
        IReadOnlyList<Embedding>?[]? generatedEmbeddings = null;

        var vectorPropertyCount = model.VectorProperties.Count;

        for (var i = 0; i < vectorPropertyCount; i++)
        {
            var vectorProperty = model.VectorProperties[i];

            if (MongoModelBuilder.IsVectorPropertyTypeValidCore(vectorProperty.Type, out _))
            {
                continue;
            }

            // We have a vector property whose type isn't natively supported - we need to generate embeddings.
            Debug.Assert(vectorProperty.EmbeddingGenerator is not null);

            // We have a property with embedding generation; materialize the records' enumerable if needed, to
            // prevent multiple enumeration.
            if (recordsList is null)
            {
                recordsList = records is IReadOnlyList<TRecord> r
                    ? r
                    : records.ToList();

                if (recordsList.Count == 0)
                {
                    return (records, null);
                }

                records = recordsList;
            }

            // TODO: Ideally we'd group together vector properties using the same generator (and with the same input and output properties),
            // and generate embeddings for them in a single batch. That's some more complexity though.
            generatedEmbeddings ??= new IReadOnlyList<Embedding>?[vectorPropertyCount];
            generatedEmbeddings[i] = await vectorProperty.GenerateEmbeddingsAsync(records.Select(r => vectorProperty.GetValueAsObject(r)), cancellationToken).ConfigureAwait(false);
        }

        return (records, generatedEmbeddings);
    }


    #region Search

    /// <inheritdoc />
    public override async IAsyncEnumerable<VectorSearchResult<TRecord>> SearchAsync<TInput>(
        TInput searchValue,
        int top,
        MEVD.VectorSearchOptions<TRecord>? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Verify.NotNull(searchValue);
        Verify.NotLessThan(top, 1);

        options ??= s_defaultVectorSearchOptions;

        if (options.IncludeVectors && _model.EmbeddingGenerationRequired)
        {
            throw new NotSupportedException(VectorDataStrings.IncludeVectorsNotSupportedWithEmbeddingGeneration);
        }

        var vectorProperty = _model.GetVectorPropertyOrSingle(options);
        var vectorArray = await GetSearchVectorArrayAsync(searchValue, vectorProperty, cancellationToken).ConfigureAwait(false);

#pragma warning disable CS0618 // VectorSearchFilter is obsolete
        var filter = options switch
        {
            { OldFilter: not null, Filter: not null } => throw new ArgumentException("Either Filter or OldFilter can be specified, but not both"),
            { OldFilter: VectorSearchFilter legacyFilter } => MongoCollectionSearchMapping.BuildLegacyFilter(legacyFilter, _model),
            { Filter: Expression<Func<TRecord, bool>> newFilter } => new MongoFilterTranslator().Translate(newFilter, _model),
            _ => null
        };
#pragma warning restore CS0618

        // Constructing a query to fetch "skip + top" total items
        // to perform skip logic locally, since skip option is not part of API.
        var itemsAmount = options.Skip + top;

        var numCandidates = _numCandidates ?? itemsAmount * MongoConstants.DefaultNumCandidatesRatio;

        var searchQuery = MongoCollectionSearchMapping.GetSearchQuery(
            vectorArray,
            _vectorIndexName,
            vectorProperty.StorageName,
            itemsAmount,
            numCandidates,
            filter);

        var projectionQuery = MongoCollectionSearchMapping.GetProjectionQuery(
            ScorePropertyName,
            DocumentPropertyName);

        List<BsonDocument> pipeline = [searchQuery, projectionQuery];

        // Add score threshold filter as a $match stage if specified
        if (options.ScoreThreshold.HasValue)
        {
            pipeline.Add(MongoCollectionSearchMapping.GetScoreThresholdMatchQuery(ScorePropertyName, options.ScoreThreshold.Value));
        }

        const string OperationName = "Aggregate";
        using var cursor = await this.RunOperationWithRetryAsync(
                OperationName,
                _maxRetries,
                _delayInMilliseconds,
                () => _mongoCollection.AggregateAsync<BsonDocument>(pipeline, cancellationToken: cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        using var errorHandlingAsyncCursor = new ErrorHandlingAsyncCursor<BsonDocument>(cursor, _collectionMetadata, OperationName);
        var mappedResults = this.EnumerateAndMapSearchResultsAsync(errorHandlingAsyncCursor,
            options.Skip,
            options.IncludeVectors,
            cancellationToken);

        await foreach (var result in mappedResults.ConfigureAwait(false))
        {
            yield return result;
        }
    }


    private static async ValueTask<float[]> GetSearchVectorArrayAsync<TInput>(TInput searchValue, VectorPropertyModel vectorProperty, CancellationToken cancellationToken)
        where TInput : notnull
    {
        if (searchValue is float[] array)
        {
            return array;
        }

        var memory = searchValue switch
        {
            ReadOnlyMemory<float> r => r,
            Embedding<float> e => e.Vector,
            _ when vectorProperty.EmbeddingGenerationDispatcher is not null
                => ((Embedding<float>)await vectorProperty.GenerateEmbeddingAsync(searchValue, cancellationToken).ConfigureAwait(false)).Vector,

            _ => vectorProperty.EmbeddingGenerator is null
                ? throw new NotSupportedException(VectorDataStrings.InvalidSearchInputAndNoEmbeddingGeneratorWasConfigured(searchValue.GetType(), MongoModelBuilder.SupportedVectorTypes))
                : throw new InvalidOperationException(VectorDataStrings.IncompatibleEmbeddingGeneratorWasConfiguredForInputType(typeof(TInput), vectorProperty.EmbeddingGenerator.GetType()))
        };

        return MemoryMarshal.TryGetArray(memory, out ArraySegment<float> segment) && segment.Count == segment.Array!.Length
            ? segment.Array
            : memory.ToArray();
    }

    #endregion Search


    /// <inheritdoc />
    public override async IAsyncEnumerable<TRecord> GetAsync(
        Expression<Func<TRecord, bool>> filter,
        int top,
        FilteredRecordRetrievalOptions<TRecord>? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Verify.NotNull(filter);
        Verify.NotLessThan(top, 1);

        options ??= new();

        // Translate the filter now, so if it fails, we throw immediately.
        var translatedFilter = new MongoFilterTranslator().Translate(filter, _model);
        SortDefinition<BsonDocument>? sortDefinition = null;
        var orderBy = options.OrderBy?.Invoke(new()).Values;

        if (orderBy is { Count: > 0 })
        {
            sortDefinition = Builders<BsonDocument>.Sort.Combine(
                orderBy.Select(pair =>
                {
                    var storageName = _model.GetDataOrKeyProperty(pair.PropertySelector).StorageName;

                    return pair.Ascending
                        ? Builders<BsonDocument>.Sort.Ascending(storageName)
                        : Builders<BsonDocument>.Sort.Descending(storageName);
                }));
        }

        using IAsyncCursor<BsonDocument> cursor = await this.FindAsync(
                translatedFilter,
                top,
                options.Skip,
                options.IncludeVectors,
                sortDefinition,
                cancellationToken)
            .ConfigureAwait(false);

        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var response in cursor.Current)
            {
                var record = _mapper.MapFromStorageToDataModel(response, options.IncludeVectors);

                yield return record;
            }
        }
    }


    /// <inheritdoc />
    public async IAsyncEnumerable<VectorSearchResult<TRecord>> HybridSearchAsync<TInput>(
        TInput searchValue,
        ICollection<string> keywords,
        int top,
        HybridSearchOptions<TRecord>? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TInput : notnull
    {
        Verify.NotLessThan(top, 1);

        options ??= s_defaultKeywordVectorizedHybridSearchOptions;
        var vectorProperty = _model.GetVectorPropertyOrSingle<TRecord>(new() { VectorProperty = options.VectorProperty });
        var vectorArray = await GetSearchVectorArrayAsync(searchValue, vectorProperty, cancellationToken).ConfigureAwait(false);
        var textDataProperty = _model.GetFullTextDataPropertyOrSingle(options.AdditionalProperty);

#pragma warning disable CS0618 // VectorSearchFilter is obsolete
        var filter = options switch
        {
            { OldFilter: not null, Filter: not null } => throw new ArgumentException("Either Filter or OldFilter can be specified, but not both"),
            { OldFilter: VectorSearchFilter legacyFilter } => MongoCollectionSearchMapping.BuildLegacyFilter(legacyFilter, _model),
            { Filter: Expression<Func<TRecord, bool>> newFilter } => new MongoFilterTranslator().Translate(newFilter, _model),
            _ => null
        };
#pragma warning restore CS0618

        // Constructing a query to fetch "skip + top" total items
        // to perform skip logic locally, since skip option is not part of API.
        var itemsAmount = options.Skip + top;

        var numCandidates = _numCandidates ?? itemsAmount * MongoConstants.DefaultNumCandidatesRatio;

        List<BsonDocument> pipeline =
        [
            .. MongoCollectionSearchMapping.GetHybridSearchPipeline(
                vectorArray,
                keywords,
                Name,
                _vectorIndexName,
                _fullTextSearchIndexName,
                vectorProperty.StorageName,
                textDataProperty.StorageName,
                ScorePropertyName,
                DocumentPropertyName,
                itemsAmount,
                numCandidates,
                filter)
        ];

        // Add score threshold filter as a $match stage if specified
        if (options.ScoreThreshold.HasValue)
        {
            pipeline.Add(MongoCollectionSearchMapping.GetScoreThresholdMatchQuery(ScorePropertyName, options.ScoreThreshold.Value));
        }

        var results = await this.RunOperationWithRetryAsync(
                "KeywordVectorizedHybridSearch",
                _maxRetries,
                _delayInMilliseconds,
                async () =>
                {
                    var cursor = await _mongoCollection
                        .AggregateAsync<BsonDocument>(pipeline, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    return EnumerateAndMapSearchResultsAsync(cursor,
                        options.Skip,
                        options.IncludeVectors,
                        cancellationToken);
                },
                cancellationToken)
            .ConfigureAwait(false);

        await foreach (var result in results.ConfigureAwait(false))
        {
            yield return result;
        }
    }


    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        Verify.NotNull(serviceType);

        return
            serviceKey is not null
                ? null
                : serviceType == typeof(VectorStoreCollectionMetadata)
                    ? _collectionMetadata
                    : serviceType == typeof(IMongoDatabase)
                        ? _mongoDatabase
                        : serviceType == typeof(IMongoCollection<BsonDocument>)
                            ? _mongoCollection
                            : serviceType.IsInstanceOfType(this)
                                ? this
                                : null;
    }


    #region private

    private async Task CreateIndexesAsync(string collectionName, CancellationToken cancellationToken)
    {
        var indexCursor = await _mongoCollection.Indexes.ListAsync(cancellationToken).ConfigureAwait(false);
        var indexes = indexCursor.ToList(cancellationToken).Select(index => index["name"].ToString()) ?? [];

        var indexArray = new BsonArray();

        // Create the vector index config if the index does not exist
        if (!indexes.Contains(_vectorIndexName))
        {
            var fieldsArray = new BsonArray();

            fieldsArray.AddRange(MongoCollectionCreateMapping.GetVectorIndexFields(_model.VectorProperties));
            fieldsArray.AddRange(MongoCollectionCreateMapping.GetFilterableDataIndexFields(_model.DataProperties));

            if (fieldsArray.Count > 0)
            {
                indexArray.Add(new BsonDocument
                {
                    { "name", _vectorIndexName },
                    { "type", "vectorSearch" },
                    { "definition", new BsonDocument { ["fields"] = fieldsArray } }
                });
            }
        }

        // Create the full text search index config if the index does not exist
        if (!indexes.Contains(_fullTextSearchIndexName))
        {
            var fieldsDocument = new BsonDocument();

            fieldsDocument.AddRange(MongoCollectionCreateMapping.GetFullTextSearchableDataIndexFields(_model.DataProperties));

            if (fieldsDocument.ElementCount > 0)
            {
                indexArray.Add(new BsonDocument
                {
                    { "name", _fullTextSearchIndexName },
                    { "type", "search" },
                    {
                        "definition", new BsonDocument
                        {
                            ["mappings"] = new BsonDocument
                            {
                                ["dynamic"] = false,
                                ["fields"] = fieldsDocument
                            }
                        }
                    }
                });
            }
        }

        // Create any missing indexes.
        if (indexArray.Count > 0)
        {
            var createIndexCommand = new BsonDocument
            {
                { "createSearchIndexes", collectionName },
                { "indexes", indexArray }
            };

            await _mongoDatabase.RunCommandAsync<BsonDocument>(createIndexCommand, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }


    private async Task<IAsyncCursor<BsonDocument>> FindAsync(
        FilterDefinition<BsonDocument> filter,
        int? top,
        int? skip,
        bool includeVectors,
        SortDefinition<BsonDocument>? sortDefinition,
        CancellationToken cancellationToken)
    {
        const string OperationName = "Find";

        ProjectionDefinitionBuilder<BsonDocument> projectionBuilder = Builders<BsonDocument>.Projection;
        ProjectionDefinition<BsonDocument>? projectionDefinition = null;

        if (!includeVectors)
        {
            foreach (var vectorPropertyName in _model.VectorProperties)
            {
                projectionDefinition = projectionDefinition is not null
                    ? projectionDefinition.Exclude(vectorPropertyName.StorageName)
                    : projectionBuilder.Exclude(vectorPropertyName.StorageName);
            }
        }

        var findOptions = projectionDefinition is not null
            ? new FindOptions<BsonDocument> { Projection = projectionDefinition, Limit = top, Skip = skip, Sort = sortDefinition }
            : new FindOptions<BsonDocument> { Limit = top, Skip = skip, Sort = sortDefinition };

        var cursor = await this.RunOperationAsync(OperationName,
                () =>
                    _mongoCollection.FindAsync(filter, findOptions, cancellationToken))
            .ConfigureAwait(false);

        return new ErrorHandlingAsyncCursor<BsonDocument>(cursor, _collectionMetadata, OperationName);
    }


    private async IAsyncEnumerable<VectorSearchResult<TRecord>> EnumerateAndMapSearchResultsAsync(
        IAsyncCursor<BsonDocument> cursor,
        int skip,
        bool includeVectors,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var skipCounter = 0;

        while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var response in cursor.Current)
            {
                if (skipCounter >= skip)
                {
                    var score = response[ScorePropertyName].AsDouble;
                    var record = _mapper.MapFromStorageToDataModel(response[DocumentPropertyName].AsBsonDocument, includeVectors);

                    yield return new VectorSearchResult<TRecord>(record, score);
                }

                skipCounter++;
            }
        }
    }


    private FilterDefinition<BsonDocument> GetFilterById(TKey id)
    {
        // Use cached key serialization info but fall back to BsonValueFactory for dynamic mapper.
        var bsonValue = _keySerializationInfo?.SerializeValue(id) ?? BsonValueFactory.Create(id);
        return Builders<BsonDocument>.Filter.Eq(MongoConstants.MongoReservedKeyPropertyName, bsonValue);
    }


    private FilterDefinition<BsonDocument> GetFilterByIds(IEnumerable<TKey> ids)
    {
        // Use cached key serialization info but fall back to BsonValueFactory for dynamic mapper.
        var bsonValues = _keySerializationInfo?.SerializeValues(ids) ?? (BsonArray)BsonValueFactory.Create(ids);
        return Builders<BsonDocument>.Filter.In(MongoConstants.MongoReservedKeyPropertyName, bsonValues);
    }


    private BsonSerializationInfo GetKeySerializationInfo()
    {
        var documentSerializer = BsonSerializer.LookupSerializer<TRecord>();

        if (documentSerializer is null)
        {
            throw new InvalidOperationException($"BsonSerializer not found for type '{typeof(TRecord)}'");
        }

        if (documentSerializer is not IBsonDocumentSerializer bsonDocumentSerializer)
        {
            throw new InvalidOperationException($"BsonSerializer for type '{typeof(TRecord)}' does not implement IBsonDocumentSerializer");
        }

        if (!bsonDocumentSerializer.TryGetMemberSerializationInfo(_model.KeyProperty.ModelName, out var keySerializationInfo))
        {
            throw new InvalidOperationException($"BsonSerializer for type '{typeof(TRecord)}' does not recognize key property {_model.KeyProperty.ModelName}");
        }

        return keySerializationInfo;
    }


    private async Task<bool> InternalCollectionExistsAsync(CancellationToken cancellationToken)
    {
        var filter = new BsonDocument("name", Name);
        var options = new ListCollectionNamesOptions { Filter = filter };

        using var cursor = await _mongoDatabase.ListCollectionNamesAsync(options, cancellationToken: cancellationToken).ConfigureAwait(false);

        return await cursor.AnyAsync(cancellationToken).ConfigureAwait(false);
    }


    private Task RunOperationAsync(string operationName, Func<Task> operation)
    {
        return MEVD.VectorStoreErrorHandler.RunOperationAsync<MongoException>(_collectionMetadata, operationName, operation);
    }


    private Task<T> RunOperationAsync<T>(string operationName, Func<Task<T>> operation)
    {
        return MEVD.VectorStoreErrorHandler.RunOperationAsync<T, MongoException>(_collectionMetadata, operationName, operation);
    }


    private Task RunOperationWithRetryAsync(
        string operationName,
        int maxRetries,
        int delayInMilliseconds,
        Func<Task> operation,
        CancellationToken cancellationToken)
    {
        return MEVD.VectorStoreErrorHandler.RunOperationWithRetryAsync<MongoException>(
            _collectionMetadata,
            operationName,
            maxRetries,
            delayInMilliseconds,
            operation,
            cancellationToken);
    }


    private async Task<T> RunOperationWithRetryAsync<T>(
        string operationName,
        int maxRetries,
        int delayInMilliseconds,
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        return await MEVD.VectorStoreErrorHandler.RunOperationWithRetryAsync<T, MongoException>(
                _collectionMetadata,
                operationName,
                maxRetries,
                delayInMilliseconds,
                operation,
                cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion


}
