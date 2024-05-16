// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.MistralAI.Client;

using System.Collections.Generic;
using System.Text.Json.Serialization;


/// <summary>
/// Response for text embedding.
/// </summary>
internal sealed class TextEmbeddingResponse : MistralResponseBase
{

    [JsonPropertyName("data")]
    public IList<MistralEmbedding>? Data { get; set; }

}
