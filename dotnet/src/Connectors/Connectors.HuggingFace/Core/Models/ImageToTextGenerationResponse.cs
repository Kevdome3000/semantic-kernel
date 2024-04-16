// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable CA1812 // Avoid uninstantiated internal classes

namespace Microsoft.SemanticKernel.Connectors.HuggingFace.Core;

using System.Collections.Generic;

internal sealed class ImageToTextGenerationResponse : List<GeneratedTextItem>;
