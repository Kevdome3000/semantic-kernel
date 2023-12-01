// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletionWithData;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;


[Experimental("SKEXP0010")]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class ChatWithDataStreamingMessage
{
    [JsonPropertyName("delta")]
    public ChatWithDataStreamingDelta Delta { get; set; } = new();

    [JsonPropertyName("end_turn")]
    public bool EndTurn { get; set; }
}
