// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.HuggingFace.Core;

using System;
using System.Collections.Generic;


/// <summary>
/// Represents the response from the Hugging Face text embedding API.
/// </summary>
/// <returns> List&lt;ReadOnlyMemory&lt;float&gt;&gt;</returns>
internal sealed class TextEmbeddingResponse : List<ReadOnlyMemory<float>>;
