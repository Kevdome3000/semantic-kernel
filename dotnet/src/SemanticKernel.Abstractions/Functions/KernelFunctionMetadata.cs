﻿// Copyright (c) Microsoft.All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SemanticKernel;
/// <summary>
/// Provides read-only metadata for a <see cref="KernelFunction"/>.
/// </summary>
public sealed class KernelFunctionMetadata
{
    /// <summary>The name of the function.</summary>
    private string _name = string.Empty;

    /// <summary>The description of the function.</summary>
    private string _description = string.Empty;

    /// <summary>The function's parameters.</summary>
    private IReadOnlyList<KernelParameterMetadata> _parameters = [];

    /// <summary>The function's return parameter.</summary>
    private KernelReturnParameterMetadata? _returnParameter;

    /// <summary>Optional metadata in addition to the named properties already available on this class.</summary>
    private ReadOnlyDictionary<string, object?>? _additionalProperties;

    /// <summary>A static empty dictionary to default to when none is provided.</summary>
    internal static readonly ReadOnlyDictionary<string, object?> s_emptyDictionary = new(new Dictionary<string, object?>());

    /// <summary>Initializes the <see cref="KernelFunctionMetadata"/> for a function with the specified name.</summary>
    /// <param name="name">The name of the function.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="name"/> was null.</exception>
    /// <exception cref="ArgumentException">An invalid name was supplied.</exception>
    public KernelFunctionMetadata(string name)
    {
        Name = name;
    }

    /// <summary>Initializes a <see cref="KernelFunctionMetadata"/> as a copy of another <see cref="KernelFunctionMetadata"/>.</summary>
    /// <exception cref="ArgumentNullException">The <paramref name="metadata"/> was null.</exception>
    /// <remarks>
    /// This creates a shallow clone of <paramref name="metadata"/>. The new instance's <see cref="Parameters"/> and
    /// <see cref="ReturnParameter"/> properties will return the same objects as in the original instance.
    /// </remarks>
    public KernelFunctionMetadata(KernelFunctionMetadata metadata)
    {
        Verify.NotNull(metadata);
        Name = metadata.Name;
        PluginName = metadata.PluginName;
        Description = metadata.Description;
        Parameters = metadata.Parameters;
        ReturnParameter = metadata.ReturnParameter;
        AdditionalProperties = metadata.AdditionalProperties;
    }

    /// <summary>Gets the name of the function.</summary>
    public string Name
    {
        get => _name;
        set
        {
            Verify.NotNull(value);
            KernelVerify.ValidFunctionName(value);
            _name = value;
        }
    }

    /// <summary>Gets the name of the plugin containing the function.</summary>
    public string? PluginName { get; set; }

    /// <summary>Gets a description of the function, suitable for use in describing the purpose to a model.</summary>
    [AllowNull]
    public string Description
    {
        get => _description;
        set => _description = value ?? string.Empty;
    }

    /// <summary>Gets the metadata for the parameters to the function.</summary>
    /// <remarks>If the function has no parameters, the returned list will be empty.</remarks>
    public IReadOnlyList<KernelParameterMetadata> Parameters
    {
        get => _parameters;
        set
        {
            Verify.NotNull(value);
            _parameters = value;
        }
    }

    /// <summary>Gets parameter metadata for the return parameter.</summary>
    /// <remarks>If the function has no return parameter, the returned value will be a default instance of a <see cref="KernelReturnParameterMetadata"/>.</remarks>
    public KernelReturnParameterMetadata ReturnParameter
    {
        get => _returnParameter ??= KernelReturnParameterMetadata.Empty;
        set
        {
            Verify.NotNull(value);
            _returnParameter = value;
        }
    }

    /// <summary>Gets optional metadata in addition to the named properties already available on this class.</summary>
    public ReadOnlyDictionary<string, object?> AdditionalProperties
    {
        get => _additionalProperties ??= s_emptyDictionary;
        set
        {
            Verify.NotNull(value);
            _additionalProperties = value;
        }
    }
}
