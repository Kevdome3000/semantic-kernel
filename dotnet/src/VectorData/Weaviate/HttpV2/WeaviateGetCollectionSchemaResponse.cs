using System.Text.Json.Serialization;
﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateGetCollectionSchemaResponse
{
    [JsonPropertyName("class")]
    public string? CollectionName { get; set; }
}
