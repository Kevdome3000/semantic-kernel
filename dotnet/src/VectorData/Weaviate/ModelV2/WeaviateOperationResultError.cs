// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateOperationResultError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
