﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Google.Core;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Logging;
using Extensions.Logging.Abstractions;
using Http;


internal abstract class ClientBase
{

    private readonly Func<Task<string>>? _bearerTokenProvider;

    private readonly ILogger _logger;

    protected HttpClient HttpClient { get; }


    protected ClientBase(
        HttpClient httpClient,
        ILogger? logger,
        Func<Task<string>> bearerTokenProvider)
        : this(httpClient, logger)
    {
        Verify.NotNull(bearerTokenProvider);
        this._bearerTokenProvider = bearerTokenProvider;
    }


    protected ClientBase(
        HttpClient httpClient,
        ILogger? logger)
    {
        Verify.NotNull(httpClient);

        this.HttpClient = httpClient;
        this._logger = logger ?? NullLogger.Instance;
    }


    protected static void ValidateMaxTokens(int? maxTokens)
    {
        // If maxTokens is null, it means that the user wants to use the default model value
        if (maxTokens is < 1)
        {
            throw new ArgumentException($"MaxTokens {maxTokens} is not valid, the value must be greater than zero");
        }
    }


    protected async Task<string> SendRequestAndGetStringBodyAsync(
        HttpRequestMessage httpRequestMessage,
        CancellationToken cancellationToken)
    {
        using var response = await this.HttpClient.SendWithSuccessCheckAsync(httpRequestMessage, cancellationToken).
            ConfigureAwait(false);

        var body = await response.Content.ReadAsStringWithExceptionMappingAsync().
            ConfigureAwait(false);

        return body;
    }


    protected async Task<HttpResponseMessage> SendRequestAndGetResponseImmediatelyAfterHeadersReadAsync(
        HttpRequestMessage httpRequestMessage,
        CancellationToken cancellationToken)
    {
        var response = await this.HttpClient.SendWithSuccessCheckAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).
            ConfigureAwait(false);

        return response;
    }


    protected static T DeserializeResponse<T>(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(body) ?? throw new JsonException("Response is null");
        }
        catch (JsonException exc)
        {
            throw new KernelException("Unexpected response from model", exc)
            {
                Data = { { "ResponseData", body } },
            };
        }
    }


    protected async Task<HttpRequestMessage> CreateHttpRequestAsync(object requestData, Uri endpoint)
    {
        var httpRequestMessage = HttpRequest.CreatePostRequest(endpoint, requestData);
        httpRequestMessage.Headers.Add("User-Agent", HttpHeaderConstant.Values.UserAgent);

        httpRequestMessage.Headers.Add(HttpHeaderConstant.Names.SemanticKernelVersion,
            HttpHeaderConstant.Values.GetAssemblyVersion(typeof(ClientBase)));

        if (this._bearerTokenProvider != null && await this._bearerTokenProvider().
                ConfigureAwait(false) is { } bearerKey)
        {
            httpRequestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerKey);
        }

        return httpRequestMessage;
    }


    protected void Log(LogLevel logLevel, string? message, params object[] args)
    {
        if (this._logger.IsEnabled(logLevel))
        {
#pragma warning disable CA2254 // Template should be a constant string.
            this._logger.Log(logLevel, message, args);
#pragma warning restore CA2254
        }
    }

}