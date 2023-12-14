// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Chroma;

using System.Net.Http;
using System.Text.Json.Serialization;


internal sealed class CreateCollectionRequest
{
    [JsonPropertyName("name")]
    public string CollectionName { get; set; }

    [JsonPropertyName("get_or_create")]
    public bool GetOrCreate => true;


    public static CreateCollectionRequest Create(string collectionName)
    {
        return new CreateCollectionRequest(collectionName);
    }


    public HttpRequestMessage Build()
    {
        return HttpRequest.CreatePostRequest("collections", this);
    }


    #region private ================================================================================

    private CreateCollectionRequest(string collectionName)
    {
        this.CollectionName = collectionName;
    }

    #endregion


}
