// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Events;

using Orchestration;


/// <summary>
/// Event arguments available to the Kernel.PromptRendered event.
/// </summary>
public class PromptRenderedEventArgs : KernelCancelEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PromptRenderedEventArgs"/> class.
    /// </summary>
    /// <param name="function">Kernel function</param>
    /// <param name="variables">Context variables related to the event</param>
    /// <param name="renderedPrompt">Rendered prompt</param>
    public PromptRenderedEventArgs(KernelFunction function, ContextVariables variables, string renderedPrompt) : base(function, variables)
    {
        this.RenderedPrompt = renderedPrompt;
    }


    /// <summary>
    /// Rendered prompt.
    /// </summary>
    public string RenderedPrompt { get; set; }
}
