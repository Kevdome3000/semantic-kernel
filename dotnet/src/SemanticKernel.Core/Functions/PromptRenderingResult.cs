// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel;

using Services;


/// <summary>
/// Contains result after prompt rendering process.
/// </summary>
internal sealed class PromptRenderingResult
{
    public IAIService AIService { get; set; }

    public string RenderedPrompt { get; set; }

    public PromptExecutionSettings? ExecutionSettings { get; set; }

    public PromptRenderedEventArgs? RenderedEventArgs { get; set; }

    public PromptRenderedContext? RenderedContext { get; set; }


    public PromptRenderingResult(IAIService aiService, string renderedPrompt)
    {
        this.AIService = aiService;
        this.RenderedPrompt = renderedPrompt;
    }
}
