// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

using System.Net.Http;


internal sealed class DeleteSchemaRequest
{
    private readonly string _class;


    private DeleteSchemaRequest(string @class)
    {
        this._class = @class;
    }


    public static DeleteSchemaRequest Create(string @class)
    {
        return new(@class);
    }


    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateDeleteRequest($"schema/{this._class}");
    }
}
