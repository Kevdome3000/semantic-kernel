// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

using System.Net.Http;


internal sealed class GetSchemaRequest
{
    public static GetSchemaRequest Create()
    {
        return new();
    }


    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateGetRequest("schema");
    }
}
