// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateQueryMatchWhereClause
{
    [JsonPropertyName("operator")]
    public string? Operator { get; set; }

    [JsonPropertyName("path")]
    public List<string> Path { get; set; } = [];

    [JsonPropertyName("valueTextArray")]
    public List<string> Values { get; set; } = [];
}
