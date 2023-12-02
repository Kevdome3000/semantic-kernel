// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.AI;

namespace Microsoft.SemanticKernel.Planning;

/// <summary>
/// Extension methods for PromptTemplateConfig
/// </summary>
internal static class PromptTemplateConfigExtensions
{
    /// <summary>
    /// Set the max_tokens request setting to be used by OpenAI models
    /// </summary>
    /// <param name="config">PromptTemplateConfig instance</param>
    /// <param name="maxTokens">Value of max tokens to set</param>
    internal static void SetMaxTokens(this PromptTemplateConfig config, int maxTokens)
    {
        PromptExecutionSettings executionSettings = new();
        if (config.ExecutionSettings.Count == 0)
        {
            config.ExecutionSettings.Add(executionSettings);
        }
        executionSettings.ExtensionData["max_tokens"] = maxTokens;
    }
}
