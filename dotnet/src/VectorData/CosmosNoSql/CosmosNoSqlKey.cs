using Microsoft.Azure.Cosmos;
using System;
﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.CosmosNoSql;

/// <summary>
/// Represents a key for Azure CosmosDB NoSQL, containing both the record key (document ID) and the partition key.
/// </summary>
/// <remarks>
/// <para>
/// Azure Cosmos DB requires both a record key (the unique document identifier within a partition) and a partition key
/// to identify a document. This struct encapsulates both values together with the Cosmos SDK's <see cref="PartitionKey"/> type.
/// </para>
/// <para>
/// For simple partition keys, use the convenience constructors that accept <see cref="string"/>, <see cref="bool"/>, or <see cref="double"/>
/// partition key values. For hierarchical partition keys (up to 3 levels), construct a <see cref="PartitionKey"/> using
/// <see cref="PartitionKeyBuilder"/> and pass it to the primary constructor.
/// </para>
/// </remarks>
// This is conceptually a record, but we're targeting .NET Standard 2.0 too.
public readonly struct CosmosNoSqlKey : IEquatable<CosmosNoSqlKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosNoSqlKey"/> struct with a document ID and partition key.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="partitionKey">The Cosmos DB partition key.</param>
    public CosmosNoSqlKey(string documentId, string partitionKey)
    {
        DocumentId = documentId;
        PartitionKey = new PartitionKey(partitionKey);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosNoSqlKey"/> struct with a document ID and partition key.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="partitionKey">The Cosmos DB partition key.</param>
    public CosmosNoSqlKey(Guid documentId, string partitionKey)
    {
        DocumentId = documentId.ToString();
        PartitionKey = new PartitionKey(partitionKey);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosNoSqlKey"/> struct with a document ID and partition key.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="partitionKey">The Cosmos DB partition key.</param>
    public CosmosNoSqlKey(string documentId, Guid partitionKey)
    {
        DocumentId = documentId;
        PartitionKey = new PartitionKey(partitionKey.ToString());
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosNoSqlKey"/> struct with a document ID and partition key.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="partitionKey">The Cosmos DB partition key.</param>
    public CosmosNoSqlKey(Guid documentId, Guid partitionKey)
    {
        DocumentId = documentId.ToString();
        PartitionKey = new PartitionKey(partitionKey.ToString());
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosNoSqlKey"/> struct with a document ID and partition key.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="partitionKey">The Cosmos DB partition key.</param>
    public CosmosNoSqlKey(string documentId, bool partitionKey)
    {
        DocumentId = documentId;
        PartitionKey = new PartitionKey(partitionKey);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosNoSqlKey"/> struct with a document ID and partition key.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="partitionKey">The Cosmos DB partition key.</param>
    public CosmosNoSqlKey(Guid documentId, bool partitionKey)
    {
        DocumentId = documentId.ToString();
        PartitionKey = new PartitionKey(partitionKey);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosNoSqlKey"/> struct with a document ID and partition key.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="partitionKey">The Cosmos DB partition key.</param>
    public CosmosNoSqlKey(string documentId, double partitionKey)
    {
        DocumentId = documentId;
        PartitionKey = new PartitionKey(partitionKey);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosNoSqlKey"/> struct with a document ID and partition key.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="partitionKey">The Cosmos DB partition key.</param>
    public CosmosNoSqlKey(Guid documentId, double partitionKey)
    {
        DocumentId = documentId.ToString();
        PartitionKey = new PartitionKey(partitionKey);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosNoSqlKey"/> struct with a document ID and partition key.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="partitionKey">The Cosmos DB partition key.</param>
    /// <remarks>
    /// Use this constructor for hierarchical partition keys or special partition key values like <see cref="PartitionKey.None"/> or <see cref="PartitionKey.Null"/>.
    /// For hierarchical partition keys, construct the <see cref="PartitionKey"/> using <see cref="PartitionKeyBuilder"/>.
    /// </remarks>
    public CosmosNoSqlKey(string documentId, PartitionKey partitionKey)
    {
        DocumentId = documentId;
        PartitionKey = partitionKey;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosNoSqlKey"/> struct with a document ID and partition key.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <param name="partitionKey">The Cosmos DB partition key.</param>
    /// <remarks>
    /// Use this constructor for hierarchical partition keys or special partition key values like <see cref="PartitionKey.None"/> or <see cref="PartitionKey.Null"/>.
    /// For hierarchical partition keys, construct the <see cref="PartitionKey"/> using <see cref="PartitionKeyBuilder"/>.
    /// </remarks>
    public CosmosNoSqlKey(Guid documentId, PartitionKey partitionKey)
    {
        DocumentId = documentId.ToString();
        PartitionKey = partitionKey;
    }


    /// <summary>
    /// Gets the document ID.
    /// </summary>
    /// <remarks>
    /// The document ID uniquely identifies a document within a partition. It is stored in the <c>id</c> property of the Cosmos DB document.
    /// </remarks>
    public string DocumentId { get; }

    /// <summary>
    /// Gets the partition key.
    /// </summary>
    /// <remarks>
    /// The partition key determines which logical partition the document belongs to.
    /// See <see href="https://learn.microsoft.com/azure/cosmos-db/partitioning-overview"/> for guidance on choosing partition keys.
    /// </remarks>
    public PartitionKey PartitionKey { get; }


    /// <inheritdoc/>
    public bool Equals(CosmosNoSqlKey other)
    {
        return Equals(DocumentId, other.DocumentId) && PartitionKey.Equals(other.PartitionKey);
    }


    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is CosmosNoSqlKey other && Equals(other);
    }


    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(DocumentId, PartitionKey);
    }


    /// <summary>
    /// Determines whether two <see cref="CosmosNoSqlKey"/> instances are equal.
    /// </summary>
    public static bool operator ==(CosmosNoSqlKey left, CosmosNoSqlKey right)
    {
        return left.Equals(right);
    }


    /// <summary>
    /// Determines whether two <see cref="CosmosNoSqlKey"/> instances are not equal.
    /// </summary>
    public static bool operator !=(CosmosNoSqlKey left, CosmosNoSqlKey right)
    {
        return !left.Equals(right);
    }
}
