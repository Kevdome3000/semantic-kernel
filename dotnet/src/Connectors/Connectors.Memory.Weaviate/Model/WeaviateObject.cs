// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

using System;
using System.Collections.Generic;


internal class WeaviateObject
{
    public string? Id { get; set; }
    public string? Class { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public ReadOnlyMemory<float> Vector { get; set; }
}
