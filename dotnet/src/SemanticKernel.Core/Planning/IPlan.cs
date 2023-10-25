// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Planning;

using System;


/// <summary>
/// Interface for standard Semantic Kernel callable plan.
/// </summary>
[Obsolete("This interface is obsoleted, use ISKFunction interface instead")]
public interface IPlan : ISKFunction
{
}
