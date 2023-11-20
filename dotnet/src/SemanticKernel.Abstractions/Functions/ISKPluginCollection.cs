// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130

// ReSharper disable once CheckNamespace - Using the main namespace
namespace Microsoft.SemanticKernel;

using System.Collections.Generic;


/// <summary>Provides a collection of <see cref="ISKPlugin"/>s.</summary>
public interface ISKPluginCollection : ICollection<ISKPlugin>, IReadOnlySKPluginCollection
{
}
