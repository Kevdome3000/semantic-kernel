﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Pinecone;

using System.Net.Http;


/// <summary>
/// Deletes an index and all its data.
/// See https://docs.pinecone.io/reference/delete_index
/// </summary>
internal sealed class DeleteIndexRequest
{
    public static DeleteIndexRequest Create(string indexName)
    {
        return new DeleteIndexRequest(indexName);
    }


    public HttpRequestMessage Build()
    {
        HttpRequestMessage request = HttpRequest.CreateDeleteRequest(
            $"/databases/{this._indexName}");

        request.Headers.Add("accept", "text/plain");

        return request;
    }


    #region private ================================================================================

    private readonly string _indexName;


    private DeleteIndexRequest(string indexName)
    {
        this._indexName = indexName;
    }

    #endregion


}
