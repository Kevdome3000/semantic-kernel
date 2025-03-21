﻿// Copyright (c) Microsoft.All rights reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.SemanticKernel;
/// <summary>Provides a builder for constructing instances of <see cref="Kernel"/>.</summary>
public sealed class KernelBuilder : IKernelBuilder, IKernelBuilderPlugins
{
    /// <summary>The collection of services to be available through the <see cref="Kernel"/>.</summary>
    private IServiceCollection? _services;

    /// <summary>Initializes a new instance of the <see cref="KernelBuilder"/>.</summary>
    public KernelBuilder()
    {
        AllowBuild = true;
    }

    /// <summary>Initializes a new instance of the <see cref="KernelBuilder"/>.</summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to wrap and use for building the <see cref="Kernel"/>.
    /// </param>
    public KernelBuilder(IServiceCollection services)
    {
        Verify.NotNull(services);

        _services = services;
        AllowBuild = true;
    }

    /// <summary>Whether to allow a call to Build.</summary>
    /// <remarks>As a minor aid to help avoid misuse, we try to prevent Build from being called on instances returned from AddKernel.</remarks>
    internal bool AllowBuild { get; }

    /// <summary>Gets the collection of services to be built into the <see cref="Kernel"/>.</summary>
    public IServiceCollection Services => _services ??= new ServiceCollection();

    /// <summary>Gets a builder for plugins to be built as services into the <see cref="Kernel"/>.</summary>
    public IKernelBuilderPlugins Plugins => this;
}
