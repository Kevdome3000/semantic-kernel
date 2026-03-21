// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

internal sealed class WeaviateDeleteObjectBatchRequest
{
    private const string ApiRoute = "batch/objects";


    [JsonConstructor]
    public WeaviateDeleteObjectBatchRequest() { }


    public WeaviateDeleteObjectBatchRequest(WeaviateQueryMatch match)
    {
        Match = match;
    }


    [JsonPropertyName("match")]
    public WeaviateQueryMatch? Match { get; set; }


    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateDeleteRequest(ApiRoute, this);
    }
}
