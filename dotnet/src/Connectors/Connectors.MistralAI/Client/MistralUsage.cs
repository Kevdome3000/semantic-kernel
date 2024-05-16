// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.MistralAI.Client;

using System.Text.Json.Serialization;


/// <summary>
/// Usage for chat completion.
/// </summary>
public class MistralUsage
{

    /// <summary>
    /// The number of tokens in the provided prompts for the completions request.
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int? PromptTokens { get; set; }

    /// <summary>
    /// The number of tokens generated across all completions emissions.
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public int? CompletionTokens { get; set; }

    /// <summary>
    /// The total number of tokens processed for the completions request and response.
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int? TotalTokens { get; set; }

}
