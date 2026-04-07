// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Chroma;

internal sealed class QueryEmbeddingsRequest
{
    [JsonIgnore]
    public string CollectionId { get; set; }

    [JsonPropertyName("query_embeddings")]
    public ReadOnlyMemory<float>[] QueryEmbeddings { get; set; }

    [JsonPropertyName("n_results")]
    public int NResults { get; set; }

    [JsonPropertyName("include")]
    public string[]? Include { get; set; }


    public static QueryEmbeddingsRequest Create(
        string collectionId,
        ReadOnlyMemory<float>[] queryEmbeddings,
        int nResults,
        string[]? include = null)
    {
        return new QueryEmbeddingsRequest(collectionId,
            queryEmbeddings,
            nResults,
            include);
    }


    public HttpRequestMessage Build()
    {
        return HttpRequest.CreatePostRequest($"collections/{CollectionId}/query", this);
    }


    #region private ================================================================================

    private QueryEmbeddingsRequest(
        string collectionId,
        ReadOnlyMemory<float>[] queryEmbeddings,
        int nResults,
        string[]? include = null)
    {
        CollectionId = collectionId;
        QueryEmbeddings = queryEmbeddings;
        NResults = nResults;
        Include = include;
    }

    #endregion


}
