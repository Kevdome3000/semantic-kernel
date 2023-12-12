// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Memory.Weaviate.Http.ApiSchema;

using System.Text.Json.Nodes;

// ReSharper disable once ClassNeverInstantiated.Global
#pragma warning disable CA1812 // 'ObjectResponseResult' is an internal class that is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it 'static' (Module in Visual Basic).
internal sealed class ObjectResponseResult
#pragma warning restore CA1812 // 'ObjectResponseResult' is an internal class that is apparently never instantiated. If so, remove the code from the assembly. If this class is intended to contain only static members, make it 'static' (Module in Visual Basic).
{
    public JsonObject? Errors { get; set; }
    public string? Status { get; set; }
}
