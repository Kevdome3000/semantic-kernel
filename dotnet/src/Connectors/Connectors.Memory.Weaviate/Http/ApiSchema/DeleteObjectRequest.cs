// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

using System.Net.Http;


internal sealed class DeleteObjectRequest
{
    public string? Class { get; set; }
    public string? Id { get; set; }


    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateDeleteRequest($"objects/{this.Class}/{this.Id}");
    }
}
