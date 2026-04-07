using System.Collections.Generic;
using System.Text.Json.Serialization;
﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateCollectionSchema
{
    [JsonConstructor]
    public WeaviateCollectionSchema(string collectionName)
    {
        CollectionName = collectionName;
    }


    [JsonPropertyName("class")]
    public string CollectionName { get; set; }

    [JsonPropertyName("vectorConfig")]
    public Dictionary<string, WeaviateCollectionSchemaVectorConfig> VectorConfigurations { get; set; } = [];

    [JsonPropertyName("properties")]
    public List<WeaviateCollectionSchemaProperty> Properties { get; set; } = [];

    [JsonPropertyName("vectorizer")]
    public string Vectorizer { get; set; } = WeaviateConstants.DefaultVectorizer;

    [JsonPropertyName("vectorIndexType")]
    public string? VectorIndexType { get; set; }

    [JsonPropertyName("vectorIndexConfig")]
    public WeaviateCollectionSchemaVectorIndexConfig? VectorIndexConfig { get; set; }
}
