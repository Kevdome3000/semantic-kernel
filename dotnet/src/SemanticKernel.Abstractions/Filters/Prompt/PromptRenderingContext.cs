﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel;

using System.Diagnostics.CodeAnalysis;


/// <summary>
/// Class with data related to prompt before rendering.
/// </summary>
[Experimental("SKEXP0004")]
public sealed class PromptRenderingContext : PromptFilterContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PromptRenderingContext"/> class.
    /// </summary>
    /// <param name="function">The <see cref="KernelFunction"/> with which this filter is associated.</param>
    /// <param name="arguments">The arguments associated with the operation.</param>
    public PromptRenderingContext(KernelFunction function, KernelArguments arguments)
        : base(function, arguments, metadata: null)
    {
    }
}