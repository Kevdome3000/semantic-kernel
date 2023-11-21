// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace - Using the main namespace
namespace Microsoft.SemanticKernel;

using System.Text.Json.Serialization;

#pragma warning restore IDE0130


/// <summary>
/// A class for describing the reponse/return type of an SKFunctionFactory in a JSON Schema friendly way.
/// </summary>
internal sealed class JsonSchemaFunctionResponse
{
    /// <summary>
    /// The response description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The response content.
    /// </summary>
    [JsonPropertyName("content")]
    public JsonSchemaFunctionContent Content { get; set; } = new JsonSchemaFunctionContent();
}
