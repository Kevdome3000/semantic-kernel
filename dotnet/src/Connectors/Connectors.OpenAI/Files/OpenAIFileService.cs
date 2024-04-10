// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Logging;
using Extensions.Logging.Abstractions;
using Http;


/// <summary>
/// File service access for OpenAI: https://api.openai.com/v1/files
/// </summary>
public sealed class OpenAIFileService
{

    private const string HeaderNameAuthorization = "Authorization";

    private const string HeaderNameAzureApiKey = "api-key";

    private const string HeaderNameOpenAIAssistant = "OpenAI-Beta";

    private const string HeaderNameUserAgent = "User-Agent";

    private const string HeaderOpenAIValueAssistant = "assistants=v1";

    private const string OpenAIApiEndpoint = "https://api.openai.com/v1/";

    private const string OpenAIApiRouteFiles = "files";

    private const string AzureOpenAIApiRouteFiles = "openai/files";

    private const string AzureOpenAIDefaultVersion = "2024-02-15-preview";

    private readonly string _apiKey;

    private readonly HttpClient _httpClient;

    private readonly ILogger _logger;

    private readonly Uri _serviceUri;

    private readonly string? _version;

    private readonly string? _organization;


    /// <summary>
    /// Create an instance of the Azure OpenAI chat completion connector
    /// </summary>
    /// <param name="endpoint">Azure Endpoint URL</param>
    /// <param name="apiKey">Azure OpenAI API Key</param>
    /// <param name="organization">OpenAI Organization Id (usually optional)</param>
    /// <param name="version">The API version to target.</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public OpenAIFileService(
        Uri endpoint,
        string apiKey,
        string? organization = null,
        string? version = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(apiKey, nameof(apiKey));

        _apiKey = apiKey;
        _logger = loggerFactory?.CreateLogger(typeof(OpenAIFileService)) ?? NullLogger.Instance;
        _httpClient = HttpClientProvider.GetHttpClient(httpClient);
        _serviceUri = new Uri(_httpClient.BaseAddress ?? endpoint, AzureOpenAIApiRouteFiles);
        _version = version ?? AzureOpenAIDefaultVersion;
        _organization = organization;
    }


    /// <summary>
    /// Create an instance of the OpenAI chat completion connector
    /// </summary>
    /// <param name="apiKey">OpenAI API Key</param>
    /// <param name="organization">OpenAI Organization Id (usually optional)</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public OpenAIFileService(
        string apiKey,
        string? organization = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(apiKey, nameof(apiKey));

        _apiKey = apiKey;
        _logger = loggerFactory?.CreateLogger(typeof(OpenAIFileService)) ?? NullLogger.Instance;
        _httpClient = HttpClientProvider.GetHttpClient(httpClient);
        _serviceUri = new Uri(_httpClient.BaseAddress ?? new Uri(OpenAIApiEndpoint), OpenAIApiRouteFiles);
        _organization = organization;
    }


    /// <summary>
    /// Remove a previously uploaded file.
    /// </summary>
    /// <param name="id">The uploaded file identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    public async Task DeleteFileAsync(string id, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(id, nameof(id));

        await ExecuteDeleteRequestAsync($"{_serviceUri}/{id}", cancellationToken).
            ConfigureAwait(false);
    }


    /// <summary>
    /// Retrieve the file content from a previously uploaded file.
    /// </summary>
    /// <param name="id">The uploaded file identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The file content as <see cref="BinaryContent"/></returns>
    /// <remarks>
    /// Files uploaded with <see cref="OpenAIFilePurpose.Assistants"/> do not support content retrieval.
    /// </remarks>
    public BinaryContent GetFileContent(string id, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(id, nameof(id));

        return new BinaryContent(() => StreamGetRequestAsync($"{_serviceUri}/{id}/content", cancellationToken));
    }


    /// <summary>
    /// Retrieve metadata for a previously uploaded file.
    /// </summary>
    /// <param name="id">The uploaded file identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The metadata associated with the specified file identifier.</returns>
    public async Task<OpenAIFileReference> GetFileAsync(string id, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(id, nameof(id));

        var result = await ExecuteGetRequestAsync<FileInfo>($"{_serviceUri}/{id}", cancellationToken).
            ConfigureAwait(false);

        return ConvertFileReference(result);
    }


    /// <summary>
    /// Retrieve metadata for all previously uploaded files.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The metadata of all uploaded files.</returns>
    public async Task<IEnumerable<OpenAIFileReference>> GetFilesAsync(CancellationToken cancellationToken = default)
    {
        var result = await ExecuteGetRequestAsync<FileInfoList>(_serviceUri.ToString(), cancellationToken).
            ConfigureAwait(false);

        return result.Data.Select(r => ConvertFileReference(r)).
            ToArray();
    }


    /// <summary>
    /// Upload a file.
    /// </summary>
    /// <param name="fileContent">The file content as <see cref="BinaryContent"/></param>
    /// <param name="settings">The upload settings</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The file metadata.</returns>
    public async Task<OpenAIFileReference> UploadContentAsync(BinaryContent fileContent, OpenAIFileUploadExecutionSettings settings, CancellationToken cancellationToken = default)
    {
        Verify.NotNull(settings, nameof(settings));

        using var formData = new MultipartFormDataContent();
        using var contentPurpose = new StringContent(ConvertPurpose(settings.Purpose));

        using var contentStream = await fileContent.GetStreamAsync().
            ConfigureAwait(false);

        using var contentFile = new StreamContent(contentStream);
        formData.Add(contentPurpose, "purpose");
        formData.Add(contentFile, "file", settings.FileName);

        var result = await ExecutePostRequestAsync<FileInfo>(_serviceUri.ToString(), formData, cancellationToken).
            ConfigureAwait(false);

        return ConvertFileReference(result);
    }


    private async Task ExecuteDeleteRequestAsync(string url, CancellationToken cancellationToken)
    {
        using var request = HttpRequest.CreateDeleteRequest(PrepareUrl(url));
        AddRequestHeaders(request);

        using var _ = await _httpClient.SendWithSuccessCheckAsync(request, cancellationToken).
            ConfigureAwait(false);
    }


    private async Task<TModel> ExecuteGetRequestAsync<TModel>(string url, CancellationToken cancellationToken)
    {
        using var request = HttpRequest.CreateGetRequest(PrepareUrl(url));
        AddRequestHeaders(request);

        using var response = await _httpClient.SendWithSuccessCheckAsync(request, cancellationToken).
            ConfigureAwait(false);

        var body = await response.Content.ReadAsStringWithExceptionMappingAsync().
            ConfigureAwait(false);

        var model = JsonSerializer.Deserialize<TModel>(body);

        return
            model ??
            throw new KernelException($"Unexpected response from {url}")
            {
                Data = { { "ResponseData", body } }
            };
    }


    private async Task<Stream> StreamGetRequestAsync(string url, CancellationToken cancellationToken)
    {
        using var request = HttpRequest.CreateGetRequest(PrepareUrl(url));
        AddRequestHeaders(request);

        var response = await _httpClient.SendWithSuccessCheckAsync(request, cancellationToken).
            ConfigureAwait(false);

        try
        {
            return
                new HttpResponseStream(
                    await response.Content.ReadAsStreamAndTranslateExceptionAsync().
                        ConfigureAwait(false),
                    response);
        }
        catch
        {
            response.Dispose();

            throw;
        }
    }


    private async Task<TModel> ExecutePostRequestAsync<TModel>(string url, HttpContent payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, PrepareUrl(url)) { Content = payload };
        AddRequestHeaders(request);

        using var response = await _httpClient.SendWithSuccessCheckAsync(request, cancellationToken).
            ConfigureAwait(false);

        var body = await response.Content.ReadAsStringWithExceptionMappingAsync().
            ConfigureAwait(false);

        var model = JsonSerializer.Deserialize<TModel>(body);

        return
            model ??
            throw new KernelException($"Unexpected response from {url}")
            {
                Data = { { "ResponseData", body } }
            };
    }


    private string PrepareUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(_version))
        {
            return url;
        }

        return $"{url}?api-version={_version}";
    }


    private void AddRequestHeaders(HttpRequestMessage request)
    {
        request.Headers.Add(HeaderNameOpenAIAssistant, HeaderOpenAIValueAssistant);
        request.Headers.Add(HeaderNameUserAgent, HttpHeaderConstant.Values.UserAgent);
        request.Headers.Add(HttpHeaderConstant.Names.SemanticKernelVersion, HttpHeaderConstant.Values.GetAssemblyVersion(typeof(OpenAIFileService)));

        if (!string.IsNullOrWhiteSpace(_version))
        {
            // Azure OpenAI
            request.Headers.Add(HeaderNameAzureApiKey, _apiKey);

            return;
        }

        // OpenAI
        request.Headers.Add(HeaderNameAuthorization, $"Bearer {_apiKey}");

        if (!string.IsNullOrEmpty(_organization))
        {
            _httpClient.DefaultRequestHeaders.Add(OpenAIClientCore.OrganizationKey, _organization);
        }
    }


    private OpenAIFileReference ConvertFileReference(FileInfo result) => new()
    {
        Id = result.Id,
        FileName = result.FileName,
        CreatedTimestamp = DateTimeOffset.FromUnixTimeSeconds(result.CreatedAt).
            UtcDateTime,
        SizeInBytes = result.Bytes ?? 0,
        Purpose = ConvertPurpose(result.Purpose)
    };


    private OpenAIFilePurpose ConvertPurpose(string purpose) =>
        purpose.ToUpperInvariant() switch
        {
            "ASSISTANTS" => OpenAIFilePurpose.Assistants,
            "FINE-TUNE" => OpenAIFilePurpose.FineTune,
            _ => throw new KernelException($"Unknown {nameof(OpenAIFilePurpose)}: {purpose}.")
        };


    private string ConvertPurpose(OpenAIFilePurpose purpose) =>
        purpose switch
        {
            OpenAIFilePurpose.Assistants => "assistants",
            OpenAIFilePurpose.FineTune => "fine-tune",
            _ => throw new KernelException($"Unknown {nameof(OpenAIFilePurpose)}: {purpose}.")
        };


    private class FileInfoList
    {

        [JsonPropertyName("data")]
        public FileInfo[] Data { get; set; } = Array.Empty<FileInfo>();

        [JsonPropertyName("object")]
        public string Object { get; set; } = "list";

    }


    private class FileInfo
    {

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = "file";

        [JsonPropertyName("bytes")]
        public int? Bytes { get; set; }

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("filename")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("purpose")]
        public string Purpose { get; set; } = string.Empty;

    }

}
