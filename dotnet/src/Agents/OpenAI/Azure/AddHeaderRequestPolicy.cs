// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.SemanticKernel.Agents.OpenAI.Azure;

using global::Azure.Core;
using global::Azure.Core.Pipeline;


/// <summary>
/// Helper class to inject headers into Azure SDK HTTP pipeline
/// </summary>
internal sealed class AddHeaderRequestPolicy(string headerName, string headerValue) : HttpPipelineSynchronousPolicy
{

    public override void OnSendingRequest(HttpMessage message) => message.Request.Headers.Add(headerName, headerValue);

}
