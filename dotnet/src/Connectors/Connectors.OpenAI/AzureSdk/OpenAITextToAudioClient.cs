// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Logging;
using Extensions.Logging.Abstractions;
using Http;


/// <summary>
/// OpenAI text-to-audio client for HTTP operations.
/// </summary>
internal sealed class OpenAITextToAudioClient
{

    private readonly ILogger _logger;

    private readonly HttpClient _httpClient;

    private readonly string _modelId;

    private readonly string _apiKey;

    private readonly string? _organization;

    /// <summary>
    /// Storage for AI service attributes.
    /// </summary>
    internal Dictionary<string, object?> Attributes { get; } = new();


    /// <summary>
    /// Creates an instance of the <see cref="OpenAITextToAudioClient"/> with API key auth.
    /// </summary>
    /// <param name="modelId">Model name</param>
    /// <param name="apiKey">OpenAI API Key</param>
    /// <param name="organization">OpenAI Organization Id (usually optional)</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="logger">The <see cref="ILogger"/> to use for logging. If null, no logging will be performed.</param>
    internal OpenAITextToAudioClient(
        string modelId,
        string apiKey,
        string? organization = null,
        HttpClient? httpClient = null,
        ILogger? logger = null)
    {
        Verify.NotNullOrWhiteSpace(modelId);
        Verify.NotNullOrWhiteSpace(apiKey);

        _modelId = modelId;
        _apiKey = apiKey;
        _organization = organization;

        _httpClient = HttpClientProvider.GetHttpClient(httpClient);
        _logger = logger ?? NullLogger.Instance;
    }


    internal async Task<IReadOnlyList<AudioContent>> GetAudioContentsAsync(
        string text,
        PromptExecutionSettings? executionSettings,
        CancellationToken cancellationToken)
    {
        OpenAITextToAudioExecutionSettings? audioExecutionSettings = OpenAITextToAudioExecutionSettings.FromExecutionSettings(executionSettings);

        Verify.NotNullOrWhiteSpace(audioExecutionSettings?.Voice);

        using var request = GetRequest(text, audioExecutionSettings);

        using var response = await SendRequestAsync(request, cancellationToken).
            ConfigureAwait(false);

        var data = await response.Content.ReadAsByteArrayAndTranslateExceptionAsync().
            ConfigureAwait(false);

        return new List<AudioContent> { new(data, _modelId) };
    }


    internal void AddAttribute(string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            Attributes.Add(key, value);
        }
    }


    #region private

    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Add("User-Agent", HttpHeaderConstant.Values.UserAgent);
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
        request.Headers.Add(HttpHeaderConstant.Names.SemanticKernelVersion, HttpHeaderConstant.Values.GetAssemblyVersion(typeof(OpenAITextToAudioClient)));

        if (!string.IsNullOrWhiteSpace(_organization))
        {
            request.Headers.Add("OpenAI-Organization", _organization);
        }

        try
        {
            return await _httpClient.SendWithSuccessCheckAsync(request, cancellationToken).
                ConfigureAwait(false);
        }
        catch (HttpOperationException ex)
        {
            _logger.LogError(
                "Error occurred on text-to-audio request execution: {ExceptionMessage}", ex.Message);

            throw;
        }
    }


    private HttpRequestMessage GetRequest(string text, OpenAITextToAudioExecutionSettings executionSettings)
    {
        const string DefaultBaseUrl = "https://api.openai.com";

        var baseUrl = !string.IsNullOrWhiteSpace(_httpClient.BaseAddress?.AbsoluteUri)
            ? _httpClient.BaseAddress!.AbsoluteUri
            : DefaultBaseUrl;

        var payload = new TextToAudioRequest(_modelId, text, executionSettings.Voice)
        {
            ResponseFormat = executionSettings.ResponseFormat,
            Speed = executionSettings.Speed
        };

        return HttpRequest.CreatePostRequest($"{baseUrl.TrimEnd('/')}/v1/audio/speech", payload);
    }

    #endregion


}
