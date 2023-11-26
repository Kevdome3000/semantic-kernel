// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.FunctionCalling;

using System;
using System.Collections.Generic;
using Azure.AI.OpenAI;


/// <summary>
///  Extension of ChatRequestSettings for use with the OpenAI connector for function calling
/// </summary>
public class FunctionCallRequestSettings : OpenAIPromptExecutionSettings
{
    /// <summary>
    ///  The function to call
    /// </summary>
    public FunctionDefinition? TargetFunctionCall { get; init; }

    /// <summary>
    ///  The functions that can be called by the LLM
    /// </summary>
    public List<FunctionDefinition>? CallableFunctions { get; init; }

    public Action<string>? EmitDamagedResponse { get; set; }
}
