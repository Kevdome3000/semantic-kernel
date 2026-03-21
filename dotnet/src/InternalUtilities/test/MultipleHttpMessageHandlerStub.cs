// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1812


internal sealed class MultipleHttpMessageHandlerStub : DelegatingHandler
{
    private int _callIteration;

    public List<HttpRequestHeaders?> RequestHeaders { get; private set; } = [];

    public List<HttpContentHeaders?> ContentHeaders { get; private set; } = [];

    public List<byte[]?> RequestContents { get; private set; } = [];

    public List<Uri?> RequestUris { get; private set; } = [];

    public List<HttpMethod?> Methods { get; private set; } = [];

    public List<HttpResponseMessage> ResponsesToReturn { get; set; } = [];


    internal HttpClient CreateHttpClient()
    {
        return new HttpClient(this, false);
    }


    internal void AddJsonResponse(string json)
    {
        ResponsesToReturn.Add(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json)
        });
    }


    internal void AddImageResponse(byte[] image)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(image)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        ResponsesToReturn.Add(response);
    }


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _callIteration++;

        Methods.Add(request.Method);
        RequestUris.Add(request.RequestUri);
        RequestHeaders.Add(request.Headers);
        ContentHeaders.Add(request.Content?.Headers);

        var content = request.Content is null
            ? null
            : await request.Content.ReadAsByteArrayAsync(cancellationToken);

        RequestContents.Add(content);

        return await Task.FromResult(ResponsesToReturn[_callIteration - 1]);
    }


    internal string? GetRequestContentAsString(int index, Encoding? encoding = null)
    {
        return RequestContents[index] is null
            ? null
            : (encoding ?? Encoding.UTF8).GetString(RequestContents[index]!);
    }
}
