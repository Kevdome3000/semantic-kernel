// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletionWithData;

using System.Text.Json.Serialization;


internal sealed class ChatWithDataSource
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = ChatWithDataSourceType.AzureCognitiveSearch.ToString();

    [JsonPropertyName("parameters")]
    public ChatWithDataSourceParameters Parameters { get; set; } = new();
}
