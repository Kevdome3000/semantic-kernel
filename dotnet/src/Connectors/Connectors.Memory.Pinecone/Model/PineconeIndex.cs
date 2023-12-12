// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Memory.Pinecone.Model;

using System.Text.Json.Serialization;


/// <summary>
/// Index entity.
/// </summary>
public sealed class PineconeIndex
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PineconeIndex" /> class.
    /// </summary>
    /// <param name="configuration">Index configuration.</param>
    /// <param name="status">Index status.</param>
    [JsonConstructor]
    public PineconeIndex(IndexDefinition configuration, IndexStatus status)
    {
        this.Configuration = configuration;
        this.Status = status;
    }


    /// <summary>
    /// How the index is configured.
    /// </summary>
    [JsonPropertyName("database")]
    public IndexDefinition Configuration { get; set; }

    /// <summary>
    /// The status of the index.
    /// </summary>
    [JsonPropertyName("status")]
    public IndexStatus Status { get; set; }
}
