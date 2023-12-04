// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Plugins.OpenApi;

using System.Net.Http;
using System.Threading.Tasks;


/// <summary>
/// Represents a delegate for serializing REST API operation response content.
/// </summary>
/// <param name="content">The operation response content.</param>
/// <returns>The serialized HTTP response content.</returns>
internal delegate Task<object> HttpResponseContentSerializer(HttpContent content);
