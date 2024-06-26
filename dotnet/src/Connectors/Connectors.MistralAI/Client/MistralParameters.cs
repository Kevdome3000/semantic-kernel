﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.MistralAI.Client;

using System.Collections.Generic;
using System.Text.Json.Serialization;


/// <summary>
/// Represents the parameters of a MistralAI function.
/// </summary>
internal sealed class MistralParameters
{

    /// <summary>
    /// Gets or sets the type of the parameters. This is always "object".
    /// </summary>
    [JsonPropertyName("type")]
    public string Type => "object";

    /// <summary>
    /// Gets or sets the JSON schema of the properties.
    /// </summary>
    [JsonPropertyName("properties")]
    public IDictionary<string, KernelJsonSchema> Properties { get; set; } = new Dictionary<string, KernelJsonSchema>();

    /// <summary>
    /// Gets or sets the list of required properties.
    /// </summary>
    [JsonPropertyName("required")]
    public IList<string> Required { get; set; } = [];

}
