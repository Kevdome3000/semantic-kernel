// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.HuggingFace;

using System;
using System.Collections.Generic;


/// <summary>
/// Represents the response from the Hugging Face text embedding API.
/// </summary>
public sealed class TextEmbeddingResponse : List<List<List<ReadOnlyMemory<float>>>>
{

}
