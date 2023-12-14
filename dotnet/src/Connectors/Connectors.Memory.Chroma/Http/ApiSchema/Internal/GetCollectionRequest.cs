// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Chroma;

using System.Net.Http;
using System.Text.Json.Serialization;


internal sealed class GetCollectionRequest
{
    [JsonIgnore]
    public string CollectionName { get; set; }


    public static GetCollectionRequest Create(string collectionName)
    {
        return new GetCollectionRequest(collectionName);
    }


    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateGetRequest($"collections/{this.CollectionName}");
    }


    #region private ================================================================================

    private GetCollectionRequest(string collectionName)
    {
        this.CollectionName = collectionName;
    }

    #endregion


}
