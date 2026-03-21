// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateQueryMatch
{
    [JsonPropertyName("class")]
    public string? CollectionName { get; set; }

    [JsonPropertyName("where")]
    public WeaviateQueryMatchWhereClause? WhereClause { get; set; }
}
