﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Pinecone;

using System.Net.Http;


/// <summary>
/// ListIndexesRequest
/// See https://docs.pinecone.io/reference/list_indexes
/// </summary>
internal sealed class ListIndexesRequest
{
    public static ListIndexesRequest Create()
    {
        return new ListIndexesRequest();
    }


    public HttpRequestMessage Build()
    {
        HttpRequestMessage request = HttpRequest.CreateGetRequest("/databases");

        request.Headers.Add("accept", "application/json; charset=utf-8");

        return request;
    }
}
