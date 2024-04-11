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
/// Azure OpenAI text-to-audio client for HTTP operations.
/// </summary>
internal sealed class AzureOpenAITextToAudioClient
{

    private readonly ILogger _logger;

    private readonly HttpClient _httpClient;

    private readonly string _deploymentName;

    private readonly string _endpoint;

    private readonly string _apiKey;

    private readonly string? _modelId;

    /// <summary>
    /// Storage for AI service attributes.
    /// </summary>
    internal Dictionary<string, object?> Attributes { get; } = [];


    /// <summary>
    /// Creates an instance of the <see cref="AzureOpenAITextToAudioClient"/> with API key auth.
    /// </summary>
    /// <param name="deploymentName">Azure OpenAI deployment name, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="endpoint">Azure OpenAI deployment URL, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="apiKey">Azure OpenAI API key, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="modelId">Azure OpenAI model id, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="logger">The <see cref="ILogger"/> to use for logging. If null, no logging will be performed.</param>
    internal AzureOpenAITextToAudioClient(
        string deploymentName,
        string endpoint,
        string apiKey,
        string? modelId = null,
        HttpClient? httpClient = null,
        ILogger? logger = null)
    {
        Verify.NotNullOrWhiteSpace(deploymentName);
        Verify.NotNullOrWhiteSpace(endpoint);
        Verify.StartsWith(endpoint, "https://", "The Azure OpenAI endpoint must start with 'https://'");
        Verify.NotNullOrWhiteSpace(apiKey);

        _deploymentName = deploymentName;
        _endpoint = endpoint;
        _apiKey = apiKey;
        _modelId = modelId;

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

        var modelId = GetModelId(audioExecutionSettings);

        using var request = GetRequest(text, modelId, audioExecutionSettings);

        using var response = await SendRequestAsync(request, cancellationToken).
            ConfigureAwait(false);

        var data = await response.Content.ReadAsByteArrayAndTranslateExceptionAsync().
            ConfigureAwait(false);

        return [new(data, modelId)];
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
        request.Headers.Add("Api-Key", _apiKey);
        request.Headers.Add(HttpHeaderConstant.Names.SemanticKernelVersion, HttpHeaderConstant.Values.GetAssemblyVersion(typeof(AzureOpenAITextToAudioClient)));

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


    private HttpRequestMessage GetRequest(string text, string modelId, OpenAITextToAudioExecutionSettings executionSettings)
    {
        const string DefaultApiVersion = "2024-02-15-preview";

        var baseUrl = !string.IsNullOrWhiteSpace(_httpClient.BaseAddress?.AbsoluteUri)
            ? _httpClient.BaseAddress!.AbsoluteUri
            : _endpoint;

        var requestUrl = $"openai/deployments/{_deploymentName}/audio/speech?api-version={DefaultApiVersion}";

        var payload = new TextToAudioRequest(modelId, text, executionSettings.Voice)
        {
            ResponseFormat = executionSettings.ResponseFormat,
            Speed = executionSettings.Speed
        };

        return HttpRequest.CreatePostRequest($"{baseUrl.TrimEnd('/')}/{requestUrl}", payload);
    }


    private string GetModelId(OpenAITextToAudioExecutionSettings executionSettings) => !string.IsNullOrWhiteSpace(_modelId) ? _modelId! :
        !string.IsNullOrWhiteSpace(executionSettings.ModelId) ? executionSettings.ModelId! :
        _deploymentName;

    #endregion


}
