// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Qdrant;

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
}
