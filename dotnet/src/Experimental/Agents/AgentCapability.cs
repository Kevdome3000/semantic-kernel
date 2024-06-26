﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Experimental.Agents;

using System;


/// <summary>
/// Description of agent capabilities.
/// </summary>
[Flags]
public enum AgentCapability
{
    /// <summary>
    /// No additional capabilities.
    /// </summary>
    None = 0,

    /// <summary>
    /// Has function / plugin capability.
    /// </summary>
    Functions,

    /// <summary>
    /// Has document / data retrieval capability.
    /// </summary>
    Retrieval,

    /// <summary>
    /// Has code-interpereter capability.
    /// </summary>
    CodeInterpreter,
}
