// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;

namespace Microsoft.SemanticKernel.Connectors.Chroma;

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
        return HttpRequest.CreateGetRequest($"collections/{CollectionName}");
    }


    #region private ================================================================================

    private GetCollectionRequest(string collectionName)
    {
        CollectionName = collectionName;
    }

    #endregion


}
