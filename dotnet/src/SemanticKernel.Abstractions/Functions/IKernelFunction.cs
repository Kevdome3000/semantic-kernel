// Copyright (c) Microsoft.All rights reserved.

using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel;
/// <summary>
/// Represents a kernel function.
/// </summary>
public interface IKernelFunction
{
    /// <summary>
    /// Gets the name of the kernel function.
    /// </summary>
    /// <remarks>
    /// This property represents the name of the kernel function.
    /// </remarks>
    string Name { get;  }

    /// Gets the name of the plugin associated with the kernel function.
    /// /
    string? PluginName { get;  }

    /// <summary>
    /// Gets the description of the kernel function.
    /// </summary>
    string Description { get;  }

    /// <summary>
    /// Provides read-only metadata for a <see cref="KernelFunction"/>.
    /// </summary>
    KernelFunctionMetadata Metadata { get; }

    /// <summary>
    /// Provides execution settings for a kernel function.
    /// </summary>
    IReadOnlyDictionary<string, PromptExecutionSettings>? ExecutionSettings { get; }

    /// <summary>
    /// Gets the method information for the underlying implementation of the kernel function.
    /// </summary>
    /// <remarks>
    /// This property provides access to the metadata and reflection details of the method
    /// that implements the functionality of the kernel function. It can be used to inspect
    /// or invoke the method dynamically when needed.
    /// </remarks>
    MethodInfo? UnderlyingMethod { get; }

    /// <summary>
    /// Invokes a <see cref="IKernelFunction"/> asynchronously with the specified arguments.
    /// </summary>
    /// <param name="kernel">The <see cref="Kernel"/> instance.</param>
    /// <param name="arguments">The optional <see cref="KernelArguments"/> instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{FunctionResult}"/> representing the asynchronous operation.</returns>
    Task<FunctionResult> InvokeAsync(Kernel kernel, KernelArguments? arguments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes the kernel function asynchronously.
    /// </summary>
    /// <param name="kernel">The kernel instance.</param>
    /// <param name="arguments">The arguments for the kernel function. Optional.</param>
    /// <param name="cancellationToken">The cancellation token. Optional.</param>
    /// <returns>A task representing the asynchronous operation. The task will complete with a FunctionResult object.</returns>
    Task<TResult?> InvokeAsync<TResult>(Kernel kernel, KernelArguments? arguments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously invokes the streaming kernel function with the provided arguments and returns an asynchronous enumerable of <see cref="StreamingKernelContent"/>.
    /// </summary>
    /// <param name="kernel">The <see cref="Kernel"/> object.</param>
    /// <param name="arguments">Optional arguments to be passed to the kernel function. Default is null.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation. Default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>An asynchronous enumerable of <see cref="StreamingKernelContent"/>.</returns>
    IAsyncEnumerable<StreamingKernelContent> InvokeStreamingAsync(Kernel kernel, KernelArguments? arguments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoke a streaming kernel function asynchronously.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
    /// <param name="kernel">The <see cref="Kernel"/> instance on which the function is invoked.</param>
    /// <param name="arguments">Optional arguments for the function.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> representing the streaming content produced by the function.</returns>
    IAsyncEnumerable<TResult> InvokeStreamingAsync<TResult>(Kernel kernel, KernelArguments? arguments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new instance of the <see cref="KernelFunction"/> class with the specified plugin name.
    /// </summary>
    /// <param name="pluginName">The name of the plugin.</param>
    /// <returns>A new instance of the <see cref="KernelFunction"/> class with the specified plugin name.</returns>
    KernelFunction Clone(string pluginName);
}
