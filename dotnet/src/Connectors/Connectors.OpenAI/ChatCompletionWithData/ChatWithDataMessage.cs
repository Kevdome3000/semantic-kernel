// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;


[Experimental("SKEXP0010")]
[Obsolete("This class is deprecated in favor of OpenAIPromptExecutionSettings.AzureChatExtensionsOptions")]
internal sealed class ChatWithDataMessage
{

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

}
