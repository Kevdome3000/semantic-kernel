// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.HuggingFace;

using System.Text.Json.Serialization;


/// <summary>
/// HTTP schema to perform completion request.
/// </summary>
public sealed class TextGenerationRequest
{
    /// <summary>
    /// Prompt to complete.
    /// </summary>
    [JsonPropertyName("inputs")]
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Enable streaming
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;
}
