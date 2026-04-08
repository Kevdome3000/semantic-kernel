// Copyright (c) Microsoft. All rights reserved.

using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.Http;
using Microsoft.SemanticKernel.Services;
using OpenAI;
#pragma warning disable IDE0005 // Using directive is unnecessary
using Microsoft.SemanticKernel.Connectors.FunctionCalling;
#pragma warning restore IDE0005 // Using directive is unnecessary

#pragma warning disable CA2208 // Instantiate argument exceptions correctly

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

/// <summary>
/// Base class for AI clients that provides common functionality for interacting with OpenAI services.
/// </summary>
internal partial class ClientCore
{
    /// <summary>
    /// White space constant.
    /// </summary>
    private const string SingleSpace = " ";

    /// <summary>
    /// Gets the attribute name used to store the organization in the <see cref="IAIService.Attributes"/> dictionary.
    /// </summary>
    internal const string OrganizationKey = "Organization";

    /// <summary>
    /// Default OpenAI API endpoint.
    /// </summary>
    private const string OpenAIV1Endpoint = "https://api.openai.com/v1";

    /// <summary>
    /// Identifier of the default model to use
    /// </summary>
    protected internal string ModelId { get; init; } = string.Empty;

    /// <summary>
    /// Non-default endpoint for OpenAI API.
    /// </summary>
    protected internal Uri? Endpoint { get; init; }

    /// <summary>
    /// Logger instance
    /// </summary>
    protected internal ILogger? Logger { get; init; }

    /// <summary>
    /// OpenAI Client
    /// </summary>
    protected internal OpenAIClient? Client { get; set; }

    /// <summary>
    /// Storage for AI service attributes.
    /// </summary>
    internal Dictionary<string, object?> Attributes { get; } = [];

    /// <summary>
    /// The function calls processor.
    /// </summary>
    protected FunctionCallsProcessor FunctionCallsProcessor { get; set; }


    /// <summary>
    /// Initializes a new instance of the <see cref="ClientCore"/> class.
    /// </summary>
    /// <param name="modelId">Model name.</param>
    /// <param name="apiKey">OpenAI API Key.</param>
    /// <param name="organizationId">OpenAI Organization Id (usually optional).</param>
    /// <param name="endpoint">OpenAI compatible API endpoint.</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="logger">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    internal ClientCore(
        string? modelId = null,
        string? apiKey = null,
        string? organizationId = null,
        Uri? endpoint = null,
        HttpClient? httpClient = null,
        ILogger? logger = null)
    {
        Logger = logger ?? NullLogger.Instance;

        FunctionCallsProcessor = new FunctionCallsProcessor(Logger);

        // Empty constructor will be used when inherited by a specialized Client.
        if (modelId is null
            && apiKey is null
            && organizationId is null
            && endpoint is null
            && httpClient is null
            && logger is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(modelId))
        {
            ModelId = modelId!;
            AddAttribute(AIServiceExtensions.ModelIdKey, modelId);
        }

        // Accepts the endpoint if provided, otherwise uses the default OpenAI endpoint.
        Endpoint = endpoint ?? httpClient?.BaseAddress;

        if (Endpoint is null)
        {
            Verify.NotNullOrWhiteSpace(apiKey); // For Public OpenAI Endpoint a key must be provided.
            Endpoint = new Uri(OpenAIV1Endpoint);
        }
        else if (string.IsNullOrEmpty(apiKey))
        {
            // Avoids an exception from OpenAI Client when a custom endpoint is provided without an API key.
            apiKey = SingleSpace;
        }

        AddAttribute(AIServiceExtensions.EndpointKey, Endpoint.ToString());

        var options = GetOpenAIClientOptions(httpClient, Endpoint);

        if (!string.IsNullOrWhiteSpace(organizationId))
        {
            options.AddPolicy(CreateRequestHeaderPolicy("OpenAI-Organization", organizationId!), PipelinePosition.PerCall);

            AddAttribute(OrganizationKey, organizationId);
        }

        Client = new OpenAIClient(new ApiKeyCredential(apiKey!), options);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="ClientCore"/> class using the specified OpenAIClient.
    /// Note: instances created this way might not have the default diagnostics settings,
    /// it's up to the caller to configure the client.
    /// </summary>
    /// <param name="modelId">OpenAI model Id</param>
    /// <param name="openAIClient">Custom <see cref="OpenAIClient"/>.</param>
    /// <param name="logger">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    internal ClientCore(
        string? modelId,
        OpenAIClient openAIClient,
        ILogger? logger = null)
    {
        // Model Id may not be required when other services. i.e: File Service.
        if (modelId is not null)
        {
            ModelId = modelId;
            AddAttribute(AIServiceExtensions.ModelIdKey, modelId);
        }

        Verify.NotNull(openAIClient);

        Logger = logger ?? NullLogger.Instance;
        Client = openAIClient;
        FunctionCallsProcessor = new FunctionCallsProcessor(Logger);
    }


    /// <summary>
    /// Logs OpenAI action details.
    /// </summary>
    /// <param name="callerMemberName">Caller member name. Populated automatically by runtime.</param>
    internal void LogActionDetails([CallerMemberName] string? callerMemberName = default)
    {
        if (Logger!.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Action: {Action}. OpenAI Model ID: {ModelId}.", callerMemberName, ModelId);
        }
    }


    /// <summary>
    /// Allows adding attributes to the client.
    /// </summary>
    /// <param name="key">Attribute key.</param>
    /// <param name="value">Attribute value.</param>
    internal void AddAttribute(string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            Attributes.Add(key, value);
        }
    }


    /// <summary>Gets options to use for an OpenAIClient</summary>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="endpoint">Endpoint for the OpenAI API.</param>
    /// <param name="orgId"></param>
    /// <returns>An instance of <see cref="OpenAIClientOptions"/>.</returns>
    internal static OpenAIClientOptions GetOpenAIClientOptions(HttpClient? httpClient, Uri? endpoint = null, string? orgId = null)
    {
        OpenAIClientOptions options = new()
        {
            UserAgentApplicationId = HttpHeaderConstant.Values.UserAgent
        };

        options.Endpoint ??= endpoint ?? httpClient?.BaseAddress;

        options.AddPolicy(CreateRequestHeaderPolicy(HttpHeaderConstant.Names.SemanticKernelVersion, HttpHeaderConstant.Values.GetAssemblyVersion(typeof(ClientCore))), PipelinePosition.PerCall);

        if (orgId is not null)
        {
            options.OrganizationId = orgId;
        }

        if (httpClient is not null)
        {
            options.Transport = new HttpClientPipelineTransport(httpClient);
            options.RetryPolicy = new ClientRetryPolicy(0); // Disable retry policy if and only if a custom HttpClient is provided.
            options.NetworkTimeout = Timeout.InfiniteTimeSpan; // Disable default timeout
        }

        return options;
    }


    /// <summary>
    /// Gets the model identifier to use for the client.
    /// </summary>
    protected virtual string GetClientModelId()
    {
        return ModelId;
    }


    /// <summary>
    /// Invokes the specified request and handles exceptions.
    /// </summary>
    /// <typeparam name="T">Type of the response.</typeparam>
    /// <param name="request">Request to invoke.</param>
    /// <returns>Returns the response.</returns>
    protected static async Task<T> RunRequestAsync<T>(Func<Task<T>> request)
    {
        try
        {
            return await request.Invoke().ConfigureAwait(false);
        }
        catch (ClientResultException e)
        {
            throw e.ToHttpOperationException();
        }
    }


    /// <summary>
    /// Invokes the specified request and handles exceptions.
    /// </summary>
    /// <typeparam name="T">Type of the response.</typeparam>
    /// <param name="request">Request to invoke.</param>
    /// <returns>Returns the response.</returns>
    protected static T RunRequest<T>(Func<T> request)
    {
        try
        {
            return request.Invoke();
        }
        catch (ClientResultException e)
        {
            throw e.ToHttpOperationException();
        }
    }


    protected static GenericActionPipelinePolicy CreateRequestHeaderPolicy(string headerName, string headerValue)
    {
        return new GenericActionPipelinePolicy(message =>
        {
            if (message?.Request?.Headers?.TryGetValue(headerName, out string? _) == false)
            {
                message.Request.Headers.Set(headerName, headerValue);
            }
        });
    }
}
