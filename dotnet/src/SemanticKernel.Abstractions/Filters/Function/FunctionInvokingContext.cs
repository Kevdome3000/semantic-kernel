// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel;

using System.Diagnostics.CodeAnalysis;


/// <summary>
/// Class with data related to function before invocation.
/// </summary>
[Experimental("SKEXP0004")]
public sealed class FunctionInvokingContext : FunctionFilterContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionInvokingContext"/> class.
    /// </summary>
    /// <param name="function">The <see cref="KernelFunction"/> with which this filter is associated.</param>
    /// <param name="arguments">The arguments associated with the operation.</param>
    public FunctionInvokingContext(KernelFunction function, KernelArguments arguments)
        : base(function, arguments, metadata: null)
    {
    }
}
