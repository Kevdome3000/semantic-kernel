// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.SemanticKernel.Planning.Structured.Action;

using Connectors.AI.OpenAI.FunctionCalling;


/// <summary>
///  A function call for use with the Action Planner
/// </summary>
public class ActionFunctionCall : FunctionCallResult
{
    /// <summary>
    /// Rationale given by the LLM for choosing the function
    /// </summary>
    public string Rationale { get; set; } = string.Empty;
}
