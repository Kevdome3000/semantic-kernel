// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Weaviate;

using System.Net.Http;
using System.Text.Json.Serialization;


internal sealed class GetClassRequest
{
    private GetClassRequest(string @class)
    {
        this.Class = @class;
    }


    /// <summary>
    ///     Name of the Weaviate class
    /// </summary>
    [JsonIgnore]
    // ReSharper disable once MemberCanBePrivate.Global
    public string Class { get; set; }


    public static GetClassRequest Create(string @class)
    {
        return new(@class);
    }


    public HttpRequestMessage Build()
    {
        return HttpRequest.CreateGetRequest($"schema/{this.Class}");
    }
}
