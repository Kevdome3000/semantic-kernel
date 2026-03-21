// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1812 // Internal class that is apparently never instantiated; this class is compiled in tests projects
internal sealed class HttpMessageHandlerStub : HttpMessageHandler
#pragma warning restore CA1812 // Internal class that is apparently never instantiated
{
    public HttpRequestHeaders? RequestHeaders { get; private set; }

    public HttpContentHeaders? ContentHeaders { get; private set; }

    public byte[]? RequestContent { get; private set; }

    public Uri? RequestUri { get; private set; }

    public HttpMethod? Method { get; private set; }

    public HttpResponseMessage ResponseToReturn { get; set; }

    public Queue<HttpResponseMessage> ResponseQueue { get; } = new();
    public byte[]? FirstMultipartContent { get; private set; }


    public HttpMessageHandlerStub()
    {
        ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, MediaTypeNames.Application.Json)
        };
    }


#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Method = request.Method;
        RequestUri = request.RequestUri;
        RequestHeaders = request.Headers;
        RequestContent = request.Content is null
            ? null
            : await request.Content.ReadAsByteArrayAsync(cancellationToken);

        if (request.Content is MultipartContent multipartContent)
        {
            FirstMultipartContent = await multipartContent.First().ReadAsByteArrayAsync(cancellationToken);
        }

        ContentHeaders = request.Content?.Headers;

        HttpResponseMessage response =
            ResponseQueue.Count == 0
                ? ResponseToReturn
                : ResponseToReturn = ResponseQueue.Dequeue();

        return response;
    }
}
