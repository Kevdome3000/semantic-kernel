// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateCollectionSchemaProperty
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("dataType")]
    public List<string> DataType { get; set; } = [];

    [JsonPropertyName("indexFilterable")]
    public bool IndexFilterable { get; set; }

    [JsonPropertyName("indexSearchable")]
    public bool IndexSearchable { get; set; }
}
