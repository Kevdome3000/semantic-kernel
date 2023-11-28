// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Services;

using System.Collections.Generic;


/// <summary>
/// Represents an AI service.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Gets the AI service attributes.
    /// </summary>
    IReadOnlyDictionary<string, string> Attributes { get; }
}
