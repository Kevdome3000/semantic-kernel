// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Plugins.OpenApi;

using System.Collections.Generic;
using System.Net.Http;


/// <summary>
/// Represents a delegate for creating HTTP content for a REST API operation.
/// </summary>
/// <param name="payload">The operation payload metadata.</param>
/// <param name="arguments">The operation arguments.</param>
/// <returns>The object and HttpContent representing the operation payload.</returns>
internal delegate (object Payload, HttpContent Content) HttpContentFactory(RestApiPayload? payload, IDictionary<string, object?> arguments);
