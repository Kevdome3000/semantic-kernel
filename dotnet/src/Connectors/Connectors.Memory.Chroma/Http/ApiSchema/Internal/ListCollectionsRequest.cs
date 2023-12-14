// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Chroma;

using System.Net.Http;


internal sealed class ListCollectionsRequest
{
    public static ListCollectionsRequest Create()
    {
        return new ListCollectionsRequest();
    }


    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateGetRequest("collections");
    }


    #region private ================================================================================

    private ListCollectionsRequest()
    {
    }

    #endregion


}
