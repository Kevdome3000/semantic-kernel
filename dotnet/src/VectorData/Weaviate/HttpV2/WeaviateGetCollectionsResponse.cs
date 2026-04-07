using System.Collections.Generic;
using System.Text.Json.Serialization;
﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateGetCollectionsResponse
{
    [JsonPropertyName("classes")]
    public List<WeaviateCollectionSchema>? Collections { get; set; }
}
