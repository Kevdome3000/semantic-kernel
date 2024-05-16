// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.MistralAI.Client;

using System.Text.Json.Serialization;


/// <summary>
/// Base class for Mistral response.
/// </summary>
internal abstract class MistralResponseBase
{

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("usage")]
    public MistralUsage? Usage { get; set; }

}
