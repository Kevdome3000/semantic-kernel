﻿// Copyright (c) Microsoft. All rights reserved.

using static Microsoft.SemanticKernel.Connectors.HuggingFace.Client.TextGenerationResponse;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes

namespace Microsoft.SemanticKernel.Connectors.HuggingFace.Client;

using System.Collections.Generic;
using System.Text.Json.Serialization;


internal sealed class ImageToTextGenerationResponse : List<GeneratedTextItem>
{

    internal sealed class GeneratedTextItem
    {

        /// <summary>
        /// The generated string
        /// </summary>
        [JsonPropertyName("generated_text")]
        public string? GeneratedText { get; set; }

    }

}