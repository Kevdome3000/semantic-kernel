// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal class WeaviateOperationResultErrors
{
    [JsonPropertyName("error")]
    public List<WeaviateOperationResultError>? Errors { get; set; }
}
