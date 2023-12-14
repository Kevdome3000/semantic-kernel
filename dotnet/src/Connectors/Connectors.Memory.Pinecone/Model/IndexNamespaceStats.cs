// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Pinecone;

using System.Text.Json.Serialization;


/// <summary>
/// Index namespace parameters.
/// </summary>
public class IndexNamespaceStats
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IndexNamespaceStats" /> class.
    /// </summary>
    /// <param name="vectorCount">vectorCount.</param>
    public IndexNamespaceStats(long vectorCount = default)
    {
        this.VectorCount = vectorCount;
    }


    /// <summary>
    /// The number of vectors in the namespace
    /// </summary>
    [JsonPropertyName("vectorCount")]
    public long VectorCount { get; }
}
