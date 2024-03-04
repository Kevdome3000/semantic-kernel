// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.HuggingFace.Client;

using System.Collections.Generic;
using System.Text.Json.Serialization;


/// <summary>
/// HTTP schema to perform embedding request.
/// </summary>
internal sealed class TextEmbeddingRequest
{

    /// <summary>
    /// Data to embed.
    /// </summary>
    [JsonPropertyName("inputs")]
    public IList<string> Inputs { get; set; } = new List<string>();

}
