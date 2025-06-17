// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Chroma;

using System.Collections.Generic;
using System.Text.Json.Serialization;


/// <summary>
/// Chroma embeddings model.
/// </summary>
public class ChromaEmbeddingsModel
{

    /// <summary>
    /// Embedding identifiers.
    /// </summary>
    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; } = [];

    /// <summary>
    /// Embedding vectors.
    /// </summary>
    [JsonPropertyName("embeddings")]
    public List<float[]> Embeddings { get; set; } = [];

    /// <summary>
    /// Embedding metadatas.
    /// </summary>
    [JsonPropertyName("metadatas")]
    public List<Dictionary<string, object>> Metadatas { get; set; } = [];

}
