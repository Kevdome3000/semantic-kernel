// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Xunit;

namespace SemanticKernel.UnitTests.Memory;

public class VolatileMemoryStoreTests
{
    private readonly VolatileMemoryStore _db;


    public VolatileMemoryStoreTests()
    {
        _db = new VolatileMemoryStore();
    }


    private int _collectionNum;


    private IEnumerable<MemoryRecord> CreateBatchRecords(int numRecords)
    {
        Assert.True(numRecords % 2 == 0, "Number of records must be even");
        Assert.True(numRecords > 0, "Number of records must be greater than 0");

        IEnumerable<MemoryRecord> records = new List<MemoryRecord>(numRecords);

        for (int i = 0; i < numRecords / 2; i++)
        {
            var testRecord = MemoryRecord.LocalRecord(
                "test" + i,
                "text" + i,
                "description" + i,
                new float[] { 1, 1, 1 });
            records = records.Append(testRecord);
        }

        for (int i = numRecords / 2; i < numRecords; i++)
        {
            var testRecord = MemoryRecord.ReferenceRecord(
                "test" + i,
                "sourceName" + i,
                "description" + i,
                new float[] { 1, 2, 3 });
            records = records.Append(testRecord);
        }

        return records;
    }


    [Fact]
    public void InitializeDbConnectionSucceeds()
    {
        // Assert
        Assert.NotNull(_db);
    }


    [Fact]
    public async Task ItCanCreateAndGetCollectionAsync()
    {
        // Arrange
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;

        // Act
        await _db.CreateCollectionAsync(collection);
        var collections = _db.GetCollectionsAsync();

        // Assert
        Assert.NotEmpty(await collections.ToArrayAsync());
        Assert.True(await collections.ContainsAsync(collection));
    }


    [Fact]
    public async Task ItHandlesExceptionsWhenCreatingCollectionAsync()
    {
        // Arrange
        string? collection = null;

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _db.CreateCollectionAsync(collection!));
    }


    [Fact]
    public async Task ItCannotInsertIntoNonExistentCollectionAsync()
    {
        // Arrange
        MemoryRecord testRecord = MemoryRecord.LocalRecord(
            "test",
            "text",
            "description",
            new float[] { 1, 2, 3 },
            key: null,
            timestamp: null);
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;

        // Assert
        await Assert.ThrowsAsync<KernelException>(async () => await _db.UpsertAsync(collection, testRecord));
    }


    [Fact]
    public async Task GetAsyncReturnsEmptyEmbeddingUnlessSpecifiedAsync()
    {
        // Arrange
        MemoryRecord testRecord = MemoryRecord.LocalRecord(
            "test",
            "text",
            "description",
            new float[] { 1, 2, 3 },
            key: null,
            timestamp: null);
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;

        // Act
        await _db.CreateCollectionAsync(collection);
        var key = await _db.UpsertAsync(collection, testRecord);
        var actualDefault = await _db.GetAsync(collection, key);
        var actualWithEmbedding = await _db.GetAsync(collection, key, true);

        // Assert
        Assert.NotNull(actualDefault);
        Assert.NotNull(actualWithEmbedding);
        Assert.True(actualDefault.Embedding.IsEmpty);
        Assert.False(actualWithEmbedding.Embedding.IsEmpty);
        Assert.NotEqual(testRecord, actualDefault);
        Assert.Equal(testRecord, actualWithEmbedding);
    }


    [Fact]
    public async Task ItCanUpsertAndRetrieveARecordWithNoTimestampAsync()
    {
        // Arrange
        MemoryRecord testRecord = MemoryRecord.LocalRecord(
            "test",
            "text",
            "description",
            new float[] { 1, 2, 3 },
            key: null,
            timestamp: null);
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;

        // Act
        await _db.CreateCollectionAsync(collection);
        var key = await _db.UpsertAsync(collection, testRecord);
        var actual = await _db.GetAsync(collection, key, true);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(testRecord, actual);
    }


    [Fact]
    public async Task ItCanUpsertAndRetrieveARecordWithTimestampAsync()
    {
        // Arrange
        MemoryRecord testRecord = MemoryRecord.LocalRecord(
            "test",
            "text",
            "description",
            new float[] { 1, 2, 3 },
            key: null,
            timestamp: DateTimeOffset.UtcNow);
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;

        // Act
        await _db.CreateCollectionAsync(collection);
        var key = await _db.UpsertAsync(collection, testRecord);
        var actual = await _db.GetAsync(collection, key, true);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(testRecord, actual);
    }


    [Fact]
    public async Task UpsertReplacesExistingRecordWithSameIdAsync()
    {
        // Arrange
        string commonId = "test";
        MemoryRecord testRecord = MemoryRecord.LocalRecord(
            commonId,
            "text",
            "description",
            new float[] { 1, 2, 3 });
        MemoryRecord testRecord2 = MemoryRecord.LocalRecord(
            commonId,
            "text2",
            "description2",
            new float[] { 1, 2, 4 });
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;

        // Act
        await _db.CreateCollectionAsync(collection);
        var key = await _db.UpsertAsync(collection, testRecord);
        var key2 = await _db.UpsertAsync(collection, testRecord2);
        var actual = await _db.GetAsync(collection, key, true);

        // Assert
        Assert.NotNull(actual);
        Assert.NotEqual(testRecord, actual);
        Assert.Equal(key, key2);
        Assert.Equal(testRecord2, actual);
    }


    [Fact]
    public async Task ExistingRecordCanBeRemovedAsync()
    {
        // Arrange
        MemoryRecord testRecord = MemoryRecord.LocalRecord(
            "test",
            "text",
            "description",
            new float[] { 1, 2, 3 });
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;

        // Act
        await _db.CreateCollectionAsync(collection);
        var key = await _db.UpsertAsync(collection, testRecord);
        await _db.RemoveAsync(collection, key);
        var actual = await _db.GetAsync(collection, key);

        // Assert
        Assert.Null(actual);
    }


    [Fact]
    public async Task RemovingNonExistingRecordDoesNothingAsync()
    {
        // Arrange
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;

        // Act
        await _db.RemoveAsync(collection, "key");
        var actual = await _db.GetAsync(collection, "key");

        // Assert
        Assert.Null(actual);
    }


    [Fact]
    public async Task ItCanListAllDatabaseCollectionsAsync()
    {
        // Arrange
        string[] testCollections = ["test_collection5", "test_collection6", "test_collection7"];
        _collectionNum += 3;
        await _db.CreateCollectionAsync(testCollections[0]);
        await _db.CreateCollectionAsync(testCollections[1]);
        await _db.CreateCollectionAsync(testCollections[2]);

        // Act
        var collections = await _db.GetCollectionsAsync().ToArrayAsync();

#pragma warning disable CA1851 // Possible multiple enumerations of 'IEnumerable' collection
        // Assert
        Assert.NotNull(collections);
        Assert.True(collections.Length != 0, "Collections is empty");
        Assert.Equal(3, collections.Length);
        Assert.True(collections.Contains(testCollections[0]),
            $"Collections does not contain the newly-created collection {testCollections[0]}");
        Assert.True(collections.Contains(testCollections[1]),
            $"Collections does not contain the newly-created collection {testCollections[1]}");
        Assert.True(collections.Contains(testCollections[2]),
            $"Collections does not contain the newly-created collection {testCollections[2]}");
    }
#pragma warning restore CA1851 // Possible multiple enumerations of 'IEnumerable' collection


    [Fact]
    public async Task GetNearestMatchesReturnsAllResultsWithNoMinScoreAsync()
    {
        // Arrange
        var compareEmbedding = new float[] { 1, 1, 1 };
        int topN = 4;
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;
        await _db.CreateCollectionAsync(collection);
        int i = 0;
        MemoryRecord testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { 1, 1, 1 });
        _ = await _db.UpsertAsync(collection, testRecord);

        i++;
        testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { -1, -1, -1 });
        _ = await _db.UpsertAsync(collection, testRecord);

        i++;
        testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { 1, 2, 3 });
        _ = await _db.UpsertAsync(collection, testRecord);

        i++;
        testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { -1, -2, -3 });
        _ = await _db.UpsertAsync(collection, testRecord);

        i++;
        testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { 1, -1, -2 });
        _ = await _db.UpsertAsync(collection, testRecord);

        // Act
        double threshold = -1;
        var topNResults = await _db.GetNearestMatchesAsync(collection,
                compareEmbedding,
                topN,
                threshold)
            .ToArrayAsync();

        // Assert
        Assert.Equal(topN, topNResults.Length);

        for (int j = 0; j < topN - 1; j++)
        {
            int compare = topNResults[j].Item2.CompareTo(topNResults[j + 1].Item2);
            Assert.True(compare >= 0);
        }
    }


    [Fact]
    public async Task GetNearestMatchAsyncReturnsEmptyEmbeddingUnlessSpecifiedAsync()
    {
        // Arrange
        var compareEmbedding = new float[] { 1, 1, 1 };
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;
        await _db.CreateCollectionAsync(collection);
        int i = 0;
        MemoryRecord testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { 1, 1, 1 });
        _ = await _db.UpsertAsync(collection, testRecord);

        i++;
        testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { -1, -1, -1 });
        _ = await _db.UpsertAsync(collection, testRecord);

        i++;
        testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { 1, 2, 3 });
        _ = await _db.UpsertAsync(collection, testRecord);

        i++;
        testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { -1, -2, -3 });
        _ = await _db.UpsertAsync(collection, testRecord);

        i++;
        testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { 1, -1, -2 });
        _ = await _db.UpsertAsync(collection, testRecord);

        // Act
        double threshold = 0.75;
        var topNResultDefault = await _db.GetNearestMatchAsync(collection, compareEmbedding, threshold);
        var topNResultWithEmbedding = await _db.GetNearestMatchAsync(collection,
            compareEmbedding,
            threshold,
            true);

        // Assert
        Assert.NotNull(topNResultDefault);
        Assert.NotNull(topNResultWithEmbedding);
        Assert.True(topNResultDefault.Value.Item1.Embedding.IsEmpty);
        Assert.False(topNResultWithEmbedding.Value.Item1.Embedding.IsEmpty);
    }


    [Fact]
    public async Task GetNearestMatchAsyncReturnsExpectedAsync()
    {
        // Arrange
        var compareEmbedding = new float[] { 1, 1, 1 };
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;
        await _db.CreateCollectionAsync(collection);
        int i = 0;
        MemoryRecord testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { 1, 1, 1 });
        _ = await _db.UpsertAsync(collection, testRecord);

        i++;
        testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { -1, -1, -1 });
        _ = await _db.UpsertAsync(collection, testRecord);

        i++;
        testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { 1, 2, 3 });
        _ = await _db.UpsertAsync(collection, testRecord);

        i++;
        testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { -1, -2, -3 });
        _ = await _db.UpsertAsync(collection, testRecord);

        i++;
        testRecord = MemoryRecord.LocalRecord(
            "test" + i,
            "text" + i,
            "description" + i,
            new float[] { 1, -1, -2 });
        _ = await _db.UpsertAsync(collection, testRecord);

        // Act
        double threshold = 0.75;
        var topNResult = await _db.GetNearestMatchAsync(collection, compareEmbedding, threshold);

        // Assert
        Assert.NotNull(topNResult);
        Assert.Equal("test0", topNResult.Value.Item1.Metadata.Id);
        Assert.True(topNResult.Value.Item2 >= threshold);
    }


    [Fact]
    public async Task GetNearestMatchesDifferentiatesIdenticalVectorsByKeyAsync()
    {
        // Arrange
        var compareEmbedding = new float[] { 1, 1, 1 };
        int topN = 4;
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;
        await _db.CreateCollectionAsync(collection);

        for (int i = 0; i < 10; i++)
        {
            MemoryRecord testRecord = MemoryRecord.LocalRecord(
                "test" + i,
                "text" + i,
                "description" + i,
                new float[] { 1, 1, 1 });
            _ = await _db.UpsertAsync(collection, testRecord);
        }

        // Act
        var topNResults = await _db.GetNearestMatchesAsync(collection,
                compareEmbedding,
                topN,
                0.75)
            .ToArrayAsync();
        IEnumerable<string> topNKeys = topNResults.Select(x => x.Item1.Key).ToImmutableSortedSet();

        // Assert
        Assert.Equal(topN, topNResults.Length);
        Assert.Equal(topN, topNKeys.Count());

        for (int i = 0; i < topNResults.Length; i++)
        {
            int compare = topNResults[i].Item2.CompareTo(0.75);
            Assert.True(compare >= 0);
        }
    }


    [Fact]
    public async Task ItCanBatchUpsertRecordsAsync()
    {
        // Arrange
        int numRecords = 10;
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;
        await _db.CreateCollectionAsync(collection);
        IEnumerable<MemoryRecord> records = CreateBatchRecords(numRecords);

        // Act
        var keys = _db.UpsertBatchAsync(collection, records);
        var resultRecords = _db.GetBatchAsync(collection, await keys.ToArrayAsync());

        // Assert
        Assert.NotNull(keys);
        Assert.Equal(numRecords, (await keys.ToArrayAsync()).Length);
        Assert.Equal(numRecords, (await resultRecords.ToArrayAsync()).Length);
    }


    [Fact]
    public async Task ItCanBatchGetRecordsAsync()
    {
        // Arrange
        int numRecords = 10;
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;
        await _db.CreateCollectionAsync(collection);
        IEnumerable<MemoryRecord> records = CreateBatchRecords(numRecords);
        var keys = _db.UpsertBatchAsync(collection, records);

        // Act
        var results = _db.GetBatchAsync(collection, await keys.ToArrayAsync());

        // Assert
        Assert.NotNull(keys);
        Assert.NotNull(results);
        Assert.Equal(numRecords, (await results.ToArrayAsync()).Length);
    }


    [Fact]
    public async Task ItCanBatchRemoveRecordsAsync()
    {
        // Arrange
        int numRecords = 10;
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;
        await _db.CreateCollectionAsync(collection);
        IEnumerable<MemoryRecord> records = CreateBatchRecords(numRecords);

        List<string> keys = [];

        await foreach (var key in _db.UpsertBatchAsync(collection, records))
        {
            keys.Add(key);
        }

        // Act
        await _db.RemoveBatchAsync(collection, keys);

        // Assert
        await foreach (var result in _db.GetBatchAsync(collection, keys))
        {
            Assert.Null(result);
        }
    }


    [Fact]
    public async Task CollectionsCanBeDeletedAsync()
    {
        // Arrange
        var collections = await _db.GetCollectionsAsync().ToArrayAsync();
#pragma warning disable CA1851 // Possible multiple enumerations of 'IEnumerable' collection
        int numCollections = collections.Length;
        Assert.True(numCollections == _collectionNum);

        // Act
        foreach (var collection in collections)
        {
            await _db.DeleteCollectionAsync(collection);
        }

        // Assert
        collections = await _db.GetCollectionsAsync().ToArrayAsync();
        numCollections = collections.Length;
        Assert.Equal(0, numCollections);
        _collectionNum = 0;
    }
#pragma warning restore CA1851 // Possible multiple enumerations of 'IEnumerable' collection


    [Fact]
    public async Task ItThrowsWhenDeletingNonExistentCollectionAsync()
    {
        // Arrange
        string collection = "test_collection" + _collectionNum;
        _collectionNum++;

        // Act
        await Assert.ThrowsAsync<KernelException>(() => _db.DeleteCollectionAsync(collection));
    }
}
