// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.SemanticKernel.Connectors.Pinecone;

/// <summary>
/// DeleteRequest
/// See https://docs.pinecone.io/reference/delete_post
/// </summary>
[Experimental("SKEXP0020")]
internal sealed class DeleteRequest
{

    /// <summary>
    /// The ids of the vectors to delete
    /// </summary>
    [JsonPropertyName("ids")]
    public IEnumerable<string>? Ids { get; set; }

    /// <summary>
    /// Whether to delete all vectors
    /// </summary>
    [JsonPropertyName("deleteAll")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DeleteAll { get; set; }

    /// <summary>
    /// The namespace to delete vectors from
    /// </summary>
    [JsonPropertyName("namespace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Namespace { get; set; }

    /// <summary>
    /// If this parameter is present, the operation only affects vectors that satisfy the filter. See https://www.pinecone.io/docs/metadata-filtering/.
    /// </summary>
    [JsonPropertyName("filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Filter { get; set; }


    public static DeleteRequest GetDeleteAllVectorsRequest()
    {
        return new DeleteRequest(true);
    }


    public static DeleteRequest ClearNamespace(string indexNamespace)
    {
        return new DeleteRequest(true)
        {
            Namespace = indexNamespace
        };
    }


    public static DeleteRequest DeleteVectors(IEnumerable<string>? ids)
    {
        return new DeleteRequest(ids);
    }


    public DeleteRequest FilterBy(Dictionary<string, object>? filter)
    {
        Filter = filter;

        return this;
    }


    public DeleteRequest FromNamespace(string? indexNamespace)
    {
        Namespace = indexNamespace;

        return this;
    }


    public DeleteRequest Clear(bool deleteAll)
    {
        DeleteAll = deleteAll;

        return this;
    }


    public HttpRequestMessage Build()
    {
        if (Filter is not null)
        {
            Filter = PineconeUtils.ConvertFilterToPineconeFilter(Filter);
        }

        HttpRequestMessage? request = HttpRequest.CreatePostRequest(
            "/vectors/delete",
            this);

        request.Headers.Add("accept", "application/json");

        return request;
    }


    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append("DeleteRequest: ");

        if (Ids is not null)
        {
            sb.Append($"Deleting {Ids.Count()} vectors, {string.Join(", ", Ids)},");
        }

        if (DeleteAll is not null)
        {
            sb.Append("Deleting All vectors,");
        }

        if (Namespace is not null)
        {
            sb.Append($"From Namespace: {Namespace}, ");
        }

        if (Filter is null)
        {
            return sb.ToString();
        }

        sb.Append("With Filter: ");

        foreach (var pair in Filter)
        {
            sb.Append($"{pair.Key}={pair.Value}, ");
        }

        return sb.ToString();
    }


    #region private ================================================================================

    private DeleteRequest(IEnumerable<string>? ids)
    {
        Ids = ids ?? [];
    }


    private DeleteRequest(bool clear)
    {
        Ids = [];
        DeleteAll = clear;
    }

    #endregion


}
