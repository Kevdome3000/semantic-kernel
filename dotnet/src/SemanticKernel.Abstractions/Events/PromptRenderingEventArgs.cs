﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Events;

using AI;
using Orchestration;


/// <summary>
/// Event arguments available to the Kernel.PromptRendering event.
/// </summary>
public class PromptRenderingEventArgs : KernelEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PromptRenderingEventArgs"/> class.
    /// </summary>
    /// <param name="function">Kernel function</param>
    /// <param name="variables">Context variables related to the event</param>
    /// <param name="requestSettings">request settings used by the AI service</param>
    public PromptRenderingEventArgs(KernelFunction function, ContextVariables variables, PromptExecutionSettings? requestSettings) : base(function, variables)
    {
        this.RequestSettings = requestSettings; // TODO clone these settings
    }


    /// <summary>
    /// Request settings for the AI service.
    /// </summary>
    public PromptExecutionSettings? RequestSettings { get; }
}