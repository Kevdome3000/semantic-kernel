// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletionWithData;

using System.Text.Json.Serialization;


internal sealed class ChatWithDataSourceParameters
{
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("indexName")]
    public string IndexName { get; set; } = string.Empty;
}
