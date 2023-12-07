// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.HuggingFace;

using System.Text.Json.Serialization;


/// <summary>
/// HTTP Schema for completion response.
/// </summary>
public sealed class TextGenerationResponse
{
    /// <summary>
    /// Completed text.
    /// </summary>
    [JsonPropertyName("generated_text")]
    public string? Text { get; set; }
}
