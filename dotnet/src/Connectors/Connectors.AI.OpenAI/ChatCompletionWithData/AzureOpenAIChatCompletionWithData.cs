// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletionWithData;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ChatCompletion;
using Extensions.Logging;
using Extensions.Logging.Abstractions;
using Http;
using SemanticKernel.AI;
using SemanticKernel.AI.ChatCompletion;
using SemanticKernel.AI.TextCompletion;
using Services;
using Text;


/// <summary>
/// Azure OpenAI Chat Completion with data client.
/// More information: <see href="https://learn.microsoft.com/en-us/azure/ai-services/openai/use-your-data-quickstart"/>
/// </summary>
public sealed class AzureOpenAIChatCompletionWithData : IChatCompletion, ITextCompletion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAIChatCompletionWithData"/> class.
    /// </summary>
    /// <param name="config">Instance of <see cref="AzureOpenAIChatCompletionWithDataConfig"/> class with completion configuration.</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">Instance of <see cref="ILoggerFactory"/> to use for logging.</param>
    public AzureOpenAIChatCompletionWithData(
        AzureOpenAIChatCompletionWithDataConfig config,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        ValidateConfig(config);

        _config = config;

        _httpClient = httpClient ?? new HttpClient(NonDisposableHttpClientHandler.Instance, false);
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(GetType()) : NullLogger.Instance;
        _attributes.Add(IAIServiceExtensions.ModelIdKey, config.CompletionModelId);
    }


    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Attributes => _attributes;


    /// <inheritdoc/>
    public ChatHistory CreateNewChat(string? instructions = null) => new OpenAIChatHistory(instructions);


    /// <inheritdoc/>
    public async Task<IReadOnlyList<IChatResult>> GetChatCompletionsAsync(
        ChatHistory chat,
        AIRequestSettings? requestSettings = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(chat);

        OpenAIRequestSettings chatRequestSettings = OpenAIRequestSettings.FromRequestSettings(requestSettings);

        ValidateMaxTokens(chatRequestSettings.MaxTokens);

        return await ExecuteCompletionRequestAsync(chat, chatRequestSettings, cancellationToken).ConfigureAwait(false);
    }


    /// <inheritdoc/>
    public IAsyncEnumerable<IChatStreamingResult> GetStreamingChatCompletionsAsync(
        ChatHistory chat,
        AIRequestSettings? requestSettings = null,
        CancellationToken cancellationToken = default)
    {
        Verify.NotNull(chat);

        OpenAIRequestSettings chatRequestSettings = OpenAIRequestSettings.FromRequestSettings(requestSettings);

        ValidateMaxTokens(chatRequestSettings.MaxTokens);

        return ExecuteCompletionStreamingRequestAsync(chat, chatRequestSettings, cancellationToken);
    }


    /// <inheritdoc/>
    public async Task<IReadOnlyList<ITextResult>> GetCompletionsAsync(
        string text,
        AIRequestSettings? requestSettings,
        CancellationToken cancellationToken = default)
    {
        OpenAIRequestSettings chatRequestSettings = OpenAIRequestSettings.FromRequestSettings(requestSettings);

        var chat = PrepareChatHistory(text, chatRequestSettings);

        return (await GetChatCompletionsAsync(chat, chatRequestSettings, cancellationToken).ConfigureAwait(false))
            .OfType<ITextResult>()
            .ToList();
    }


    /// <inheritdoc/>
    public async IAsyncEnumerable<ITextStreamingResult> GetStreamingCompletionsAsync(
        string text,
        AIRequestSettings? requestSettings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        OpenAIRequestSettings chatRequestSettings = OpenAIRequestSettings.FromRequestSettings(requestSettings);

        var chat = PrepareChatHistory(text, chatRequestSettings);

        IAsyncEnumerable<IChatStreamingResult> results = GetStreamingChatCompletionsAsync(chat, chatRequestSettings, cancellationToken);

        await foreach (var result in results)
        {
            yield return (ITextStreamingResult)result;
        }
    }


    #region private ================================================================================

    private const string DefaultApiVersion = "2023-06-01-preview";

    private readonly AzureOpenAIChatCompletionWithDataConfig _config;

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly Dictionary<string, string> _attributes = new();


    private void ValidateConfig(AzureOpenAIChatCompletionWithDataConfig config)
    {
        Verify.NotNull(config);

        Verify.NotNullOrWhiteSpace(config.CompletionModelId);
        Verify.NotNullOrWhiteSpace(config.CompletionEndpoint);
        Verify.NotNullOrWhiteSpace(config.CompletionApiKey);
        Verify.NotNullOrWhiteSpace(config.DataSourceEndpoint);
        Verify.NotNullOrWhiteSpace(config.DataSourceApiKey);
        Verify.NotNullOrWhiteSpace(config.DataSourceIndex);
    }


    private static void ValidateMaxTokens(int? maxTokens)
    {
        if (maxTokens.HasValue && maxTokens < 1)
        {
            throw new SKException($"MaxTokens {maxTokens} is not valid, the value must be greater than zero");
        }
    }


    private async Task<IReadOnlyList<IChatResult>> ExecuteCompletionRequestAsync(
        ChatHistory chat,
        OpenAIRequestSettings requestSettings,
        CancellationToken cancellationToken = default)
    {
        using var request = GetRequest(chat, requestSettings, false);
        using var response = await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);

        var body = await response.Content.ReadAsStringWithExceptionMappingAsync().ConfigureAwait(false);

        var chatWithDataResponse = DeserializeResponse<ChatWithDataResponse>(body);

        return chatWithDataResponse.Choices.Select(choice => new ChatWithDataResult(chatWithDataResponse, choice)).ToList();
    }


    private async IAsyncEnumerable<IChatStreamingResult> ExecuteCompletionStreamingRequestAsync(
        ChatHistory chat,
        OpenAIRequestSettings requestSettings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = GetRequest(chat, requestSettings, true);
        using var response = await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);

        await foreach (var result in GetStreamingResultsAsync(response))
        {
            yield return result;
        }
    }


    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        request.Headers.Add("User-Agent", HttpHeaderValues.UserAgent);
        request.Headers.Add("Api-Key", _config.CompletionApiKey);

        try
        {
            return await _httpClient.SendWithSuccessCheckAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpOperationException ex)
        {
            _logger.LogError(
                "Error occurred on chat completion with data request execution: {ExceptionMessage}", ex.Message);

            throw;
        }
    }


    private async IAsyncEnumerable<IChatStreamingResult> GetStreamingResultsAsync(HttpResponseMessage response)
    {
        const string ServerEventPayloadPrefix = "data:";

        using var stream = await response.Content.ReadAsStreamAndTranslateExceptionAsync().ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var body = await reader.ReadLineAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(body))
            {
                continue;
            }

            if (body.StartsWith(ServerEventPayloadPrefix, StringComparison.Ordinal))
            {
                body = body.Substring(ServerEventPayloadPrefix.Length);
            }

            var chatWithDataResponse = DeserializeResponse<ChatWithDataStreamingResponse>(body);

            foreach (var choice in chatWithDataResponse.Choices)
            {
                yield return new ChatWithDataStreamingResult(chatWithDataResponse, choice);
            }
        }
    }


    private T DeserializeResponse<T>(string body)
    {
        var response = Json.Deserialize<T>(body);

        if (response is null)
        {
            const string ErrorMessage = "Error occurred on chat completion with data response deserialization";

            _logger.LogError(ErrorMessage);

            throw new SKException(ErrorMessage);
        }

        return response;
    }


    private HttpRequestMessage GetRequest(
        ChatHistory chat,
        OpenAIRequestSettings requestSettings,
        bool isStreamEnabled)
    {
        var payload = new ChatWithDataRequest
        {
            Temperature = requestSettings.Temperature,
            TopP = requestSettings.TopP,
            IsStreamEnabled = isStreamEnabled,
            StopSequences = requestSettings.StopSequences,
            MaxTokens = requestSettings.MaxTokens,
            PresencePenalty = requestSettings.PresencePenalty,
            FrequencyPenalty = requestSettings.FrequencyPenalty,
            TokenSelectionBiases = requestSettings.TokenSelectionBiases,
            DataSources = GetDataSources(),
            Messages = GetMessages(chat)
        };

        return HttpRequest.CreatePostRequest(GetRequestUri(), payload);
    }


    private List<ChatWithDataSource> GetDataSources() => new()
    {
        new()
        {
            Parameters = new ChatWithDataSourceParameters
            {
                Endpoint = _config.DataSourceEndpoint,
                ApiKey = _config.DataSourceApiKey,
                IndexName = _config.DataSourceIndex
            }
        }
    };


    private List<ChatWithDataMessage> GetMessages(ChatHistory chat)
    {
        return chat
            .Select(message => new ChatWithDataMessage
            {
                Role = message.Role.Label,
                Content = message.Content
            })
            .ToList();
    }


    private ChatHistory PrepareChatHistory(string text, OpenAIRequestSettings requestSettings)
    {
        var chat = CreateNewChat(requestSettings.ChatSystemPrompt);

        chat.AddUserMessage(text);

        return chat;
    }


    private string GetRequestUri()
    {
        const string EndpointUriFormat = "{0}/openai/deployments/{1}/extensions/chat/completions?api-version={2}";

        var apiVersion = _config.CompletionApiVersion;

        if (string.IsNullOrWhiteSpace(apiVersion))
        {
            apiVersion = DefaultApiVersion;
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            EndpointUriFormat,
            _config.CompletionEndpoint.TrimEnd('/'),
            _config.CompletionModelId,
            apiVersion);
    }

    #endregion


}
