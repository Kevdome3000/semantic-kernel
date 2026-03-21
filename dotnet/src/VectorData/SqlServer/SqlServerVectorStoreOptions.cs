// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.SqlServer;

/// <summary>
/// Options for creating a <see cref="SqlServerVectorStore"/>.
/// </summary>
public sealed class SqlServerVectorStoreOptions
{
    internal static readonly SqlServerVectorStoreOptions Defaults = new();


    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerVectorStoreOptions"/> class.
    /// </summary>
    public SqlServerVectorStoreOptions()
    {
    }


    internal SqlServerVectorStoreOptions(SqlServerVectorStoreOptions? source)
    {
        Schema = source?.Schema;
        EmbeddingGenerator = source?.EmbeddingGenerator;
    }


    /// <summary>
    /// Gets or sets the database schema.
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets the default embedding generator to use when generating vectors embeddings with this vector store.
    /// </summary>
    public IEmbeddingGenerator? EmbeddingGenerator { get; set; }
}
