﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Plugins.OpenApi;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Http;


/// <summary>
/// Runs REST API operation represented by RestApiOperation model class.
/// </summary>
internal sealed class RestApiOperationRunner
{

    private const string MediaTypeApplicationJson = "application/json";

    private const string MediaTypeTextPlain = "text/plain";

    private const string DefaultResponseKey = "default";

    /// <summary>
    /// HTTP request method.
    /// </summary>
    private const string HttpRequestMethod = "http.request.method";

    /// <summary>
    /// The HTTP request payload body.
    /// </summary>
    private const string HttpRequestBody = "http.request.body";

    /// <summary>
    /// Absolute URL describing a network resource according to RFC3986.
    /// </summary>
    private const string UrlFull = "url.full";

    /// <summary>
    /// List of payload builders/factories.
    /// </summary>
    private readonly Dictionary<string, HttpContentFactory> _payloadFactoryByMediaType;

    /// <summary>
    /// A dictionary containing the content type as the key and the corresponding content serializer as the value.
    /// </summary>
    private static readonly Dictionary<string, HttpResponseContentSerializer> s_serializerByContentType = new()
    {
        {
            "image", async (content) => await content.ReadAsByteArrayAndTranslateExceptionAsync().
                ConfigureAwait(false)
        },
        {
            "text", async (content) => await content.ReadAsStringWithExceptionMappingAsync().
                ConfigureAwait(false)
        },
        {
            "application/json", async (content) => await content.ReadAsStringWithExceptionMappingAsync().
                ConfigureAwait(false)
        },
        {
            "application/xml", async (content) => await content.ReadAsStringWithExceptionMappingAsync().
                ConfigureAwait(false)
        }
    };

    /// <summary>
    /// An instance of the HttpClient class.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Delegate for authorizing the HTTP request.
    /// </summary>
    private readonly AuthenticateRequestAsyncCallback _authCallback;

    /// <summary>
    /// Request-header field containing information about the user agent originating the request
    /// </summary>
    private readonly string? _userAgent;

    /// <summary>
    /// Determines whether the operation payload is constructed dynamically based on operation payload metadata.
    /// If false, the operation payload must be provided via the 'payload' property.
    /// </summary>
    private readonly bool _enableDynamicPayload;

    /// <summary>
    /// Determines whether payload parameters are resolved from the arguments by
    /// full name (parameter name prefixed with the parent property name).
    /// </summary>
    private readonly bool _enablePayloadNamespacing;


    /// <summary>
    /// Creates an instance of the <see cref="RestApiOperationRunner"/> class.
    /// </summary>
    /// <param name="httpClient">An instance of the HttpClient class.</param>
    /// <param name="authCallback">Optional callback for adding auth data to the API requests.</param>
    /// <param name="userAgent">Optional request-header field containing information about the user agent originating the request.</param>
    /// <param name="enableDynamicPayload">Determines whether the operation payload is constructed dynamically based on operation payload metadata.
    /// If false, the operation payload must be provided via the 'payload' property.
    /// </param>
    /// <param name="enablePayloadNamespacing">Determines whether payload parameters are resolved from the arguments by
    /// full name (parameter name prefixed with the parent property name).</param>
    public RestApiOperationRunner(
        HttpClient httpClient,
        AuthenticateRequestAsyncCallback? authCallback = null,
        string? userAgent = null,
        bool enableDynamicPayload = false,
        bool enablePayloadNamespacing = false)
    {
        this._httpClient = httpClient;
        this._userAgent = userAgent ?? HttpHeaderConstant.Values.UserAgent;
        this._enableDynamicPayload = enableDynamicPayload;
        this._enablePayloadNamespacing = enablePayloadNamespacing;

        // If no auth callback provided, use empty function
        if (authCallback is null)
        {
            this._authCallback = (_, __) => Task.CompletedTask;
        }
        else
        {
            this._authCallback = authCallback;
        }

        this._payloadFactoryByMediaType = new()
        {
            { MediaTypeApplicationJson, this.BuildJsonPayload },
            { MediaTypeTextPlain, this.BuildPlainTextPayload }
        };
    }


    /// <summary>
    /// Executes the specified <paramref name="operation"/> asynchronously, using the provided <paramref name="arguments"/>.
    /// </summary>
    /// <param name="operation">The REST API operation to execute.</param>
    /// <param name="arguments">The dictionary of arguments to be passed to the operation.</param>
    /// <param name="options">Options for REST API operation run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task execution result.</returns>
    public Task<RestApiOperationResponse> RunAsync(
        RestApiOperation operation,
        KernelArguments arguments,
        RestApiOperationRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var url = this.BuildsOperationUrl(operation, arguments, options?.ServerUrlOverride, options?.ApiHostUrl);

        var headers = operation.BuildHeaders(arguments);

        var operationPayload = this.BuildOperationPayload(operation, arguments);

        return this.SendAsync(url, operation.Method, headers, operationPayload.Payload,
            operationPayload.Content, operation.Responses.ToDictionary(item => item.Key, item => item.Value.Schema), options, cancellationToken);
    }


    #region private

    /// <summary>
    /// Sends an HTTP request.
    /// </summary>
    /// <param name="url">The url to send request to.</param>
    /// <param name="method">The HTTP request method.</param>
    /// <param name="headers">Headers to include into the HTTP request.</param>
    /// <param name="payload">HTTP request payload.</param>
    /// <param name="requestContent">HTTP request content.</param>
    /// <param name="expectedSchemas">The dictionary of expected response schemas.</param>
    /// <param name="options">Options for REST API operation run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Response content and content type</returns>
    private async Task<RestApiOperationResponse> SendAsync(
        Uri url,
        HttpMethod method,
        IDictionary<string, string>? headers = null,
        object? payload = null,
        HttpContent? requestContent = null,
        IDictionary<string, KernelJsonSchema?>? expectedSchemas = null,
        RestApiOperationRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var requestMessage = new HttpRequestMessage(method, url);

#if NET5_0_OR_GREATER
        requestMessage.Options.Set(OpenApiKernelFunctionContext.KernelFunctionContextKey, new OpenApiKernelFunctionContext(options?.Kernel, options?.KernelFunction, options?.KernelArguments));
#else
        requestMessage.Properties.Add(OpenApiKernelFunctionContext.KernelFunctionContextKey, new OpenApiKernelFunctionContext(options?.Kernel, options?.KernelFunction, options?.KernelArguments));
#endif

        await this._authCallback(requestMessage, cancellationToken).
            ConfigureAwait(false);

        if (requestContent is not null)
        {
            requestMessage.Content = requestContent;
        }

        requestMessage.Headers.Add("User-Agent", !string.IsNullOrWhiteSpace(this._userAgent)
            ? this._userAgent
            : HttpHeaderConstant.Values.UserAgent);

        requestMessage.Headers.Add(HttpHeaderConstant.Names.SemanticKernelVersion, HttpHeaderConstant.Values.GetAssemblyVersion(typeof(RestApiOperationRunner)));

        if (headers is not null)
        {
            foreach (var header in headers)
            {
                requestMessage.Headers.Add(header.Key, header.Value);
            }
        }

        try
        {
            using var responseMessage = await this._httpClient.SendWithSuccessCheckAsync(requestMessage, cancellationToken).
                ConfigureAwait(false);

            var response = await SerializeResponseContentAsync(requestMessage, payload, responseMessage).
                ConfigureAwait(false);

            response.ExpectedSchema ??= GetExpectedSchema(expectedSchemas, responseMessage.StatusCode);

            return response;
        }
        catch (HttpOperationException ex)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ex.RequestMethod = requestMessage.Method.Method;
            ex.RequestUri = requestMessage.RequestUri;
            ex.RequestPayload = payload;
#pragma warning restore CS0618 // Type or member is obsolete

            ex.Data.Add(HttpRequestMethod, requestMessage.Method.Method);
            ex.Data.Add(UrlFull, requestMessage.RequestUri?.ToString());
            ex.Data.Add(HttpRequestBody, payload);

            throw;
        }
        catch (OperationCanceledException ex)
        {
            ex.Data.Add(HttpRequestMethod, requestMessage.Method.Method);
            ex.Data.Add(UrlFull, requestMessage.RequestUri?.ToString());
            ex.Data.Add(HttpRequestBody, payload);

            throw;
        }
        catch (KernelException ex)
        {
            ex.Data.Add(HttpRequestMethod, requestMessage.Method.Method);
            ex.Data.Add(UrlFull, requestMessage.RequestUri?.ToString());
            ex.Data.Add(HttpRequestBody, payload);

            throw;
        }
    }


    /// <summary>
    /// Serializes the response content of an HTTP request.
    /// </summary>
    /// <param name="request">The HttpRequestMessage associated with the HTTP request.</param>
    /// <param name="payload">The payload sent in the HTTP request.</param>
    /// <param name="responseMessage">The HttpResponseMessage object containing the response content to be serialized.</param>
    /// <returns>The serialized content.</returns>
    private static async Task<RestApiOperationResponse> SerializeResponseContentAsync(HttpRequestMessage request, object? payload, HttpResponseMessage responseMessage)
    {
        if (responseMessage.StatusCode == HttpStatusCode.NoContent)
        {
            return new RestApiOperationResponse(null, null)
            {
                RequestMethod = request.Method.Method,
                RequestUri = request.RequestUri,
                RequestPayload = payload,
            };
        }

        var contentType = responseMessage.Content.Headers.ContentType;

        var mediaType = contentType?.MediaType ?? throw new KernelException("No media type available.");

        // Obtain the content serializer by media type (e.g., text/plain, application/json, image/jpg)
        if (!s_serializerByContentType.TryGetValue(mediaType, out var serializer))
        {
            // Split the media type into a primary-type and a sub-type
            var mediaTypeParts = mediaType.Split('/');

            if (mediaTypeParts.Length != 2)
            {
                throw new KernelException($"The string `{mediaType}` is not a valid media type.");
            }

            var primaryMediaType = mediaTypeParts.First();

            // Try to obtain the content serializer by the primary type (e.g., text, application, image)
            if (!s_serializerByContentType.TryGetValue(primaryMediaType, out serializer))
            {
                throw new KernelException($"The content type `{mediaType}` is not supported.");
            }
        }

        // Serialize response content and return it
        var serializedContent = await serializer.Invoke(responseMessage.Content).
            ConfigureAwait(false);

        return new RestApiOperationResponse(serializedContent, contentType!.ToString())
        {
            RequestMethod = request.Method.Method,
            RequestUri = request.RequestUri,
            RequestPayload = payload,
        };
    }


    /// <summary>
    /// Builds operation payload.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <param name="arguments">The operation payload arguments.</param>
    /// <returns>The raw operation payload and the corresponding HttpContent.</returns>
    private (object? Payload, HttpContent? Content) BuildOperationPayload(RestApiOperation operation, IDictionary<string, object?> arguments)
    {
        if (operation.Payload is null && !arguments.ContainsKey(RestApiOperation.PayloadArgumentName))
        {
            return (null, null);
        }

        var mediaType = operation.Payload?.MediaType;

        if (string.IsNullOrEmpty(mediaType))
        {
            if (!arguments.TryGetValue(RestApiOperation.ContentTypeArgumentName, out object? fallback) || fallback is not string mediaTypeFallback)
            {
                throw new KernelException($"No media type is provided for the {operation.Id} operation.");
            }

            mediaType = mediaTypeFallback;
        }

        if (!this._payloadFactoryByMediaType.TryGetValue(mediaType!, out var payloadFactory))
        {
            throw new KernelException($"The media type {mediaType} of the {operation.Id} operation is not supported by {nameof(RestApiOperationRunner)}.");
        }

        return payloadFactory.Invoke(operation.Payload, arguments);
    }


    /// <summary>
    /// Builds "application/json" payload.
    /// </summary>
    /// <param name="payloadMetadata">The payload meta-data.</param>
    /// <param name="arguments">The payload arguments.</param>
    /// <returns>The JSON payload the corresponding HttpContent.</returns>
    private (object? Payload, HttpContent Content) BuildJsonPayload(RestApiOperationPayload? payloadMetadata, IDictionary<string, object?> arguments)
    {
        // Build operation payload dynamically
        if (this._enableDynamicPayload)
        {
            if (payloadMetadata is null)
            {
                throw new KernelException("Payload can't be built dynamically due to the missing payload metadata.");
            }

            var payload = this.BuildJsonObject(payloadMetadata.Properties, arguments);

            return (payload, new StringContent(payload.ToJsonString(), Encoding.UTF8, MediaTypeApplicationJson));
        }

        // Get operation payload content from the 'payload' argument if dynamic payload building is not required.
        if (!arguments.TryGetValue(RestApiOperation.PayloadArgumentName, out object? argument) || argument is not string content)
        {
            throw new KernelException($"No payload is provided by the argument '{RestApiOperation.PayloadArgumentName}'.");
        }

        return (content, new StringContent(content, Encoding.UTF8, MediaTypeApplicationJson));
    }


    /// <summary>
    /// Builds a JSON object from a list of RestAPI operation payload properties.
    /// </summary>
    /// <param name="properties">The properties.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="propertyNamespace">The namespace to add to the property name.</param>
    /// <returns>The JSON object.</returns>
    private JsonObject BuildJsonObject(IList<RestApiOperationPayloadProperty> properties, IDictionary<string, object?> arguments, string? propertyNamespace = null)
    {
        var result = new JsonObject();

        foreach (var propertyMetadata in properties)
        {
            var argumentName = this.GetArgumentNameForPayload(propertyMetadata.Name, propertyNamespace);

            if (propertyMetadata.Type == "object")
            {
                var node = this.BuildJsonObject(propertyMetadata.Properties, arguments, argumentName);
                result.Add(propertyMetadata.Name, node);

                continue;
            }

            if (arguments.TryGetValue(argumentName, out object? propertyValue) && propertyValue is not null)
            {
                result.Add(propertyMetadata.Name, OpenApiTypeConverter.Convert(propertyMetadata.Name, propertyMetadata.Type, propertyValue));

                continue;
            }

            if (propertyMetadata.IsRequired)
            {
                throw new KernelException($"No argument is found for the '{propertyMetadata.Name}' payload property.");
            }
        }

        return result;
    }


    /// <summary>
    /// Gets the expected schema for the specified status code.
    /// </summary>
    /// <param name="expectedSchemas">The dictionary of expected response schemas.</param>
    /// <param name="statusCode">The status code.</param>
    /// <returns>The expected schema for the given status code.</returns>
    private static KernelJsonSchema? GetExpectedSchema(IDictionary<string, KernelJsonSchema?>? expectedSchemas, HttpStatusCode statusCode)
    {
        KernelJsonSchema? matchingResponse = null;

        if (expectedSchemas is not null)
        {
            var statusCodeKey = ((int)statusCode).ToString(CultureInfo.InvariantCulture);

            // Exact Match
            matchingResponse = expectedSchemas.FirstOrDefault(r => r.Key == statusCodeKey).
                Value;

            // Wildcard match e.g. 2XX
            matchingResponse ??= expectedSchemas.FirstOrDefault(r => r.Key is { Length: 3 } key && key[0] == statusCodeKey[0] && key[1] == 'X' && key[2] == 'X').
                Value;

            // Default
            matchingResponse ??= expectedSchemas.FirstOrDefault(r => r.Key == DefaultResponseKey).
                Value;
        }

        return matchingResponse;
    }


    /// <summary>
    /// Builds "text/plain" payload.
    /// </summary>
    /// <param name="payloadMetadata">The payload meta-data.</param>
    /// <param name="arguments">The payload arguments.</param>
    /// <returns>The text payload and corresponding HttpContent.</returns>
    private (object? Payload, HttpContent Content) BuildPlainTextPayload(RestApiOperationPayload? payloadMetadata, IDictionary<string, object?> arguments)
    {
        if (!arguments.TryGetValue(RestApiOperation.PayloadArgumentName, out object? argument) || argument is not string payload)
        {
            throw new KernelException($"No argument is found for the '{RestApiOperation.PayloadArgumentName}' payload content.");
        }

        return (payload, new StringContent(payload, Encoding.UTF8, MediaTypeTextPlain));
    }


    /// <summary>
    /// Retrieves the argument name for a payload property.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="propertyNamespace">The namespace to add to the property name (optional).</param>
    /// <returns>The argument name for the payload property.</returns>
    private string GetArgumentNameForPayload(string propertyName, string? propertyNamespace)
    {
        if (!this._enablePayloadNamespacing)
        {
            return propertyName;
        }

        return string.IsNullOrEmpty(propertyNamespace)
            ? propertyName
            : $"{propertyNamespace}.{propertyName}";
    }


    /// <summary>
    /// Builds operation Url.
    /// </summary>
    /// <param name="operation">The REST API operation.</param>
    /// <param name="arguments">The operation arguments.</param>
    /// <param name="serverUrlOverride">Override for REST API operation server url.</param>
    /// <param name="apiHostUrl">The URL of REST API host.</param>
    /// <returns>The operation Url.</returns>
    private Uri BuildsOperationUrl(
        RestApiOperation operation,
        IDictionary<string, object?> arguments,
        Uri? serverUrlOverride = null,
        Uri? apiHostUrl = null)
    {
        var url = operation.BuildOperationUrl(arguments, serverUrlOverride, apiHostUrl);

        return new UriBuilder(url) { Query = operation.BuildQueryString(arguments) }.Uri;
    }

    #endregion


}
