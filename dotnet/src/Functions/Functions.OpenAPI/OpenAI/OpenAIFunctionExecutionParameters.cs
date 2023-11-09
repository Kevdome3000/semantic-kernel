// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Functions.OpenAPI.OpenAI;

using System;
using System.Net.Http;
using Diagnostics;
using Extensions;


/// <summary>
/// OpenAI function execution parameters
/// </summary>
public class OpenAIFunctionExecutionParameters : OpenApiFunctionExecutionParameters
{
    /// <summary>
    /// Callback for adding Open AI authentication data to HTTP requests.
    /// </summary>
    public new OpenAIAuthenticateRequestAsyncCallback? AuthCallback { get; set; }


    /// <inheritdoc/>
    public OpenAIFunctionExecutionParameters(
        HttpClient? httpClient = null,
        OpenAIAuthenticateRequestAsyncCallback? authCallback = null,
        Uri? serverUrlOverride = null,
        string userAgent = Telemetry.HttpUserAgent,
        bool ignoreNonCompliantErrors = false,
        bool enableDynamicOperationPayload = false,
        bool enablePayloadNamespacing = false) : base(httpClient, null, serverUrlOverride, userAgent, ignoreNonCompliantErrors, enableDynamicOperationPayload, enablePayloadNamespacing)
    {
        this.AuthCallback = authCallback;
    }
}
