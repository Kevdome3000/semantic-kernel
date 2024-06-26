﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Chroma;

using System.Net.Http;
using System.Text.Json.Serialization;


internal sealed class DeleteCollectionRequest
{
    [JsonIgnore]
    public string CollectionName { get; set; }


    public static DeleteCollectionRequest Create(string collectionName)
    {
        return new DeleteCollectionRequest(collectionName);
    }


    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateDeleteRequest($"collections/{this.CollectionName}");
    }


    #region private ================================================================================

    private DeleteCollectionRequest(string collectionName)
    {
        this.CollectionName = collectionName;
    }

    #endregion


}
