// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

using System.Diagnostics.CodeAnalysis;


/// <summary>
/// Defines the purpose associated with the uploaded file.
/// </summary>
[Experimental("SKEXP0010")]
public enum OpenAIFilePurpose
{

    /// <summary>
    /// File to be used by assistants for model processing.
    /// </summary>
    Assistants,

    /// <summary>
    /// File to be used by fine-tuning jobs.
    /// </summary>
    FineTune,

}
