// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.OpenAI.Core.AzureSdk;

using System;
using Azure.Core;
using Azure.Core.Pipeline;


internal sealed class CustomHostPipelinePolicy : HttpPipelineSynchronousPolicy
{

    private readonly Uri _endpoint;


    internal CustomHostPipelinePolicy(Uri endpoint)
    {
        this._endpoint = endpoint;
    }


    public override void OnSendingRequest(HttpMessage message)
    {
        // Update current host to provided endpoint
        message.Request?.Uri.Reset(this._endpoint);
    }

}
