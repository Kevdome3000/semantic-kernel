// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Functions.UnitTests.OpenApi;

internal sealed class HttpMessageHandlerStub : DelegatingHandler
{

    public HttpRequestHeaders? RequestHeaders { get; private set; }

    public HttpContentHeaders? ContentHeaders { get; private set; }

    public byte[]? RequestContent { get; private set; }

    public Uri? RequestUri { get; private set; }

    public HttpMethod? Method { get; private set; }

    public HttpResponseMessage ResponseToReturn { get; set; }


    public HttpMessageHandlerStub()
    {
        ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, MediaTypeNames.Application.Json)
        };
    }


    public HttpMessageHandlerStub(Stream responseToReturn)
    {
        ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(responseToReturn)
        };
    }


    public void ResetResponse()
    {
        ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, MediaTypeNames.Application.Json)
        };
    }


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Method = request.Method;
        RequestUri = request.RequestUri;
        RequestHeaders = request.Headers;

        RequestContent = request.Content is null
            ? null
            : await request.Content.ReadAsByteArrayAsync(cancellationToken);

        ContentHeaders = request.Content?.Headers;

        return await Task.FromResult(ResponseToReturn);
    }

}
