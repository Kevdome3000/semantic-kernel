// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;

using Azure.Core;
using Azure.Core.Pipeline;


/// <summary>
/// Helper class to inject headers into Azure SDK HTTP pipeline
/// </summary>
internal sealed class AddHeaderRequestPolicy : HttpPipelineSynchronousPolicy
{
    private readonly string _headerName;
    private readonly string _headerValue;


    public AddHeaderRequestPolicy(string headerName, string headerValue)
    {
        _headerName = headerName;
        _headerValue = headerValue;
    }


    public override void OnSendingRequest(HttpMessage message)
    {
        message.Request.Headers.Add(_headerName, _headerValue);
    }
}
