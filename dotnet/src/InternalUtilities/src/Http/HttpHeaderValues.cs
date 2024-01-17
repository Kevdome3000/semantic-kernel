// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Http;

using System.Diagnostics.CodeAnalysis;


/// <summary>Provides HTTP header values for common purposes.</summary>
[ExcludeFromCodeCoverage]
internal static class HttpHeaderValues
{
    /// <summary>User agent string to use for all HTTP requests issued by Semantic Kernel.</summary>
    public static string UserAgent => "Semantic-Kernel";
}
