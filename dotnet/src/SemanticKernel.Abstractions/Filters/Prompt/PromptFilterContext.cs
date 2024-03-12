// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel;

using System.Collections.Generic;


/// <summary>
/// Base class with data related to prompt rendering.
/// </summary>
public abstract class PromptFilterContext
{

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptFilterContext"/> class.
    /// </summary>
    /// <param name="function">The <see cref="KernelFunction"/> with which this filter is associated.</param>
    /// <param name="arguments">The arguments associated with the operation.</param>
    /// <param name="metadata">A dictionary of metadata associated with the operation.</param>
    internal PromptFilterContext(KernelFunction function, KernelArguments arguments, IReadOnlyDictionary<string, object?>? metadata)
    {
        Verify.NotNull(function);
        Verify.NotNull(arguments);

        Function = function;
        Arguments = arguments;
        Metadata = metadata;
    }


    /// <summary>
    /// Gets the <see cref="KernelFunction"/> with which this filter is associated.
    /// </summary>
    public KernelFunction Function { get; }

    /// <summary>
    /// Gets the arguments associated with the operation.
    /// </summary>
    public KernelArguments Arguments { get; }

    /// <summary>
    /// Gets a dictionary of metadata associated with the operation.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Metadata { get; }

}
