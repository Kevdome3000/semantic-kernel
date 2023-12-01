// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel;

using System.Text.Json.Serialization;


/// <summary>
/// A class to describe the content of a response/return type from an KernelFunctionFactory, in a JSON Schema friendly way.
/// </summary>
internal sealed class JsonSchemaFunctionContent
{
    /// <summary>
    /// The JSON Schema for applivation/json responses.
    /// </summary>
    [JsonPropertyName("application/json")]
    public JsonSchemaResponse JsonResponse { get; } = new JsonSchemaResponse();
}
