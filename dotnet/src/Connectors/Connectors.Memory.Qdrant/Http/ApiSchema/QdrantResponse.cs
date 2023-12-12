// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Memory.Qdrant.Http.ApiSchema;

using System.Text.Json.Serialization;


/// <summary>
/// Base class for Qdrant response schema.
/// </summary>
internal abstract class QdrantResponse
{
    /// <summary>
    /// Response status
    /// </summary>
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }

    /// <summary>
    /// Response time
    /// </summary>
    [JsonPropertyName("time")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Time { get; set; }
}
