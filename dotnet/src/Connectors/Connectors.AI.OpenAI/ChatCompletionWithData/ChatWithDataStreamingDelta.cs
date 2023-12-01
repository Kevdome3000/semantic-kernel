// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletionWithData;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;


[Experimental("SKEXP0010")]
internal sealed class ChatWithDataStreamingDelta
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
