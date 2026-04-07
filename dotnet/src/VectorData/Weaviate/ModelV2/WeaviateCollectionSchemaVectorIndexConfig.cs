using System.Text.Json.Serialization;
﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateCollectionSchemaVectorIndexConfig
{
    [JsonPropertyName("distance")]
    public string? Distance { get; set; }
}
