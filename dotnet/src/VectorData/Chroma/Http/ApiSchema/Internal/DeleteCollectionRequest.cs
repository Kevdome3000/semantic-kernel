// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Chroma;

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
        return HttpRequest.CreateDeleteRequest($"collections/{CollectionName}");
    }


    #region private ================================================================================

    private DeleteCollectionRequest(string collectionName)
    {
        CollectionName = collectionName;
    }

    #endregion


}
