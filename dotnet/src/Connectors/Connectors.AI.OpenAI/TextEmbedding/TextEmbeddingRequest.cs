// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;


/// <summary>
/// A request to create embedding vector representing input text
/// </summary>
[Experimental("SKEXP0011")]
public abstract class TextEmbeddingRequest
{
    /// <summary>
    /// Input to embed
    /// </summary>
    [JsonPropertyName("input")]
    public IList<string> Input { get; set; } = new List<string>();
}


/// <summary>
/// An OpenAI embedding request
/// </summary>
[Experimental("SKEXP0011")]
public sealed class OpenAITextEmbeddingRequest : TextEmbeddingRequest
{
    /// <summary>
    /// Embedding model ID
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}


/// <summary>
/// An Azure OpenAI embedding request
/// </summary>
[Experimental("SKEXP0011")]
public sealed class AzureOpenAITextEmbeddingRequest : TextEmbeddingRequest
{
}
