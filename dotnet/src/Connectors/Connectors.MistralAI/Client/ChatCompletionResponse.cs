// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.MistralAI.Client;

using System.Collections.Generic;
using System.Text.Json.Serialization;


/// <summary>
/// Response for chat completion.
/// </summary>
internal sealed class ChatCompletionResponse : MistralResponseBase
{

    [JsonPropertyName("created")]
    public int? Created { get; set; }

    [JsonPropertyName("choices")]
    public IList<MistralChatChoice>? Choices { get; set; }

}
