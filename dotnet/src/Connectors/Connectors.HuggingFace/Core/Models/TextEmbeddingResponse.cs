﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.HuggingFace.Core;

using System;
using System.Collections.Generic;


/// <summary>
/// Represents the response from the Hugging Face text embedding API.
/// </summary>
internal sealed class TextEmbeddingResponse : List<List<List<ReadOnlyMemory<float>>>>;
