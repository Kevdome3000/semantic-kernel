﻿// Copyright (c) Microsoft.All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.AI;

#pragma warning disable CA1716 // Identifiers should not match keywords

namespace Microsoft.SemanticKernel;
/// <summary>Represents a plugin that may be registered with a <see cref="Kernel"/>.</summary>
/// <remarks>
/// A plugin is a named read-only collection of functions. There is a many-to-many relationship between
/// plugins and functions: a plugin may contain any number of functions, and a function may
/// exist in any number of plugins.
/// </remarks>
[DebuggerDisplay("Name = {Name}, Functions = {FunctionCount}")]
[DebuggerTypeProxy(typeof(TypeProxy))]
public abstract class KernelPlugin : IEnumerable<KernelFunction>
{
    /// <summary>Initializes the new plugin from the provided name, description, and function collection.</summary>
    /// <param name="name">The name for the plugin.</param>
    /// <param name="description">A description of the plugin.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is an invalid plugin name.</exception>
    protected KernelPlugin(string name, string? description = null)
    {
        KernelVerify.ValidPluginName(name);

        Name = name;

        Description = !string.IsNullOrWhiteSpace(description)
            ? description!
            : "";
    }

    /// <summary>Gets the name of the plugin.</summary>
    public string Name { get; }

    /// <summary>Gets a description of the plugin.</summary>
    public string Description { get; }

    /// <summary>Gets the function in the plugin with the specified name.</summary>
    /// <param name="functionName">The name of the function.</param>
    /// <returns>The function.</returns>
    /// <exception cref="KeyNotFoundException">The plugin does not contain a function with the specified name.</exception>
    public KernelFunction this[string functionName] =>
        TryGetFunction(functionName, out KernelFunction? function)
            ? function
            : throw new KeyNotFoundException($"The plugin does not contain a function with the specified name. Plugin name - '{Name}', function name - '{functionName}'.");

    /// <summary>Gets whether the plugin contains a function with the specified name.</summary>
    /// <param name="functionName">The name of the function.</param>
    /// <returns>true if the plugin contains the specified function; otherwise, false.</returns>
    public bool Contains(string functionName)
    {
        Verify.NotNull(functionName);

        return TryGetFunction(functionName, out _);
    }

    /// <summary>Gets whether the plugin contains a function.</summary>
    /// <param name="function">The function.</param>
    /// <returns>true if the plugin contains the specified function; otherwise, false.</returns>
    public bool Contains(KernelFunction function)
    {
        Verify.NotNull(function);

        return Contains(function.Name);
    }

    /// <summary>Gets the number of functions in this plugin.</summary>
    public abstract int FunctionCount { get; }

    /// <summary>Finds a function in the plugin by name.</summary>
    /// <param name="name">The name of the function to find.</param>
    /// <param name="function">If the plugin contains the requested function, the found function instance; otherwise, null.</param>
    /// <returns>true if the function was found in the plugin; otherwise, false.</returns>
    public abstract bool TryGetFunction(string name, [NotNullWhen(true)] out KernelFunction? function);

    /// <summary>Gets a collection of <see cref="KernelFunctionMetadata"/> instances, one for every function in this plugin.</summary>
    /// <returns>A list of metadata over every function in this plugin.</returns>
    public IList<KernelFunctionMetadata> GetFunctionsMetadata()
    {
        List<KernelFunctionMetadata> metadata = new(FunctionCount);

        foreach (KernelFunction function in this)
        {
            metadata.Add(function.Metadata);
        }

        return metadata;
    }

    /// <inheritdoc/>
    public abstract IEnumerator<KernelFunction> GetEnumerator();

    /// <summary>Produces a clone of every <see cref="KernelFunction"/> in the <see cref="KernelPlugin"/> to be used as a lower-level <see cref="AIFunction"/> abstraction.</summary>
    /// <remarks>
    /// Once this function was cloned as a <see cref="AIFunction"/>, the Name will be prefixed by the <see cref="KernelFunction.PluginName"/> i.e: PluginName_FunctionName.
    /// </remarks>
    /// <param name="kernel">The default <see cref="Kernel"/> to be used when the <see cref="AIFunction"/> is invoked.</param>
    /// <returns>An enumerable clone of <see cref="AIFunction"/> instances, for each <see cref="KernelFunction"/> in this plugin.</returns>
    [Experimental("SKEXP0001")]
    public IEnumerable<AIFunction> AsAIFunctions(Kernel? kernel = null)
    {
        foreach (KernelFunction function in this)
        {
            var functionClone = function.WithKernel(kernel);
            yield return functionClone;
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


    /// <summary>Debugger type proxy for the kernel plugin.</summary>
    private sealed class TypeProxy(KernelPlugin plugin)
    {
        private readonly KernelPlugin _plugin = plugin;

        public string Name => _plugin.Name;

        public string Description => _plugin.Description;

        public KernelFunction[] Functions => [.. _plugin.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)];
    }
}
