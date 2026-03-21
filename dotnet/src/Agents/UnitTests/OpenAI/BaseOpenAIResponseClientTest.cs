// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net.Http;
using OpenAI;
using OpenAI.Responses;

namespace SemanticKernel.Agents.UnitTests.OpenAI;

/// <summary>
/// Base tests which use <see cref="ResponsesClient"/>
/// </summary>
public class BaseOpenAIResponseClientTest : IDisposable
{
    internal MultipleHttpMessageHandlerStub MessageHandlerStub { get; }
    internal HttpClient HttpClient { get; }
    internal ResponsesClient Client { get; }


    internal BaseOpenAIResponseClientTest()
    {
        MessageHandlerStub = new MultipleHttpMessageHandlerStub();
        HttpClient = new HttpClient(MessageHandlerStub, false);

        var clientOptions = new OpenAIClientOptions
        {
            Transport = new HttpClientPipelineTransport(HttpClient)
        };
        Client = new ResponsesClient(new ApiKeyCredential("apiKey"), clientOptions);
    }


    /// <inheritdoc />
    public void Dispose()
    {
        MessageHandlerStub.Dispose();
        HttpClient.Dispose();

        GC.SuppressFinalize(this);
    }
}
