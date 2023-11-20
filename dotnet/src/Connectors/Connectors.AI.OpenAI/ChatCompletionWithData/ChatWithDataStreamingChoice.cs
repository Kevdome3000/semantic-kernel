// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletionWithData;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;


[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for JSON deserialization")]
internal sealed class ChatWithDataStreamingChoice
{
    [JsonPropertyName("messages")]
    public IList<ChatWithDataStreamingMessage> Messages { get; set; } = Array.Empty<ChatWithDataStreamingMessage>();
}
