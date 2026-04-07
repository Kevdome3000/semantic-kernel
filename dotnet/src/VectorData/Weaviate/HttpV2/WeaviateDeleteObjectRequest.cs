using System.Net.Http;
using System.Text.Json.Serialization;
using System;
﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateDeleteObjectRequest(string collectionName, Guid id)
{
    private const string ApiRoute = "objects";

    [JsonIgnore]
    public string CollectionName { get; set; } = collectionName;

    [JsonIgnore]
    public Guid Id { get; set; } = id;


    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateDeleteRequest($"{ApiRoute}/{CollectionName}/{Id}");
    }
}
