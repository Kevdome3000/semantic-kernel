// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.Qdrant;

using System.Net.Http;


internal static class HttpHandlers
{
    public static HttpClientHandler CheckCertificateRevocation { get; } = new HttpClientHandler
    {
        CheckCertificateRevocationList = false
    };
}
