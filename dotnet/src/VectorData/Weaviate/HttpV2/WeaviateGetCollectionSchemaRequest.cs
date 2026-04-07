using System.Net.Http;
using System.Text.Json.Serialization;
﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateGetCollectionSchemaRequest(string collectionName)
{
    private const string ApiRoute = "schema";

    [JsonIgnore]
    public string CollectionName { get; set; } = collectionName;


    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateGetRequest($"{ApiRoute}/{CollectionName}");
    }
}
