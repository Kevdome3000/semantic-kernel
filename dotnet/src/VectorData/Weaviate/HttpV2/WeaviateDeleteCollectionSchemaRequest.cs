using System.Net.Http;
using System.Text.Json.Serialization;
﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateDeleteCollectionSchemaRequest(string collectionName)
{
    private const string ApiRoute = "schema";

    [JsonIgnore]
    public string CollectionName { get; set; } = collectionName;


    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateDeleteRequest($"{ApiRoute}/{CollectionName}");
    }
}
