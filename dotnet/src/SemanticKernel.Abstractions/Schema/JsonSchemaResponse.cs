// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace - Using the main namespace
namespace Microsoft.SemanticKernel;

using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning restore IDE0130


/// <summary>
/// A class to describe the content schma of a response/return type from an SKFunction, in a Json Schema friendly way.
/// </summary>
public sealed class JsonSchemaResponse
{
    /// <summary>
    /// The Json Schema
    /// </summary>
    [JsonPropertyName("schema")]
    public JsonDocument? Schema { get; set; }
}
