// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Experimental.Orchestration.Abstractions;

using System.Threading.Tasks;
using SemanticKernel.Orchestration;


/// <summary>
/// Flow executor interface
/// </summary>
public interface IFlowExecutor
{
    /// <summary>
    /// Execute the <see cref="Flow"/>
    /// </summary>
    /// <param name="flow">Flow</param>
    /// <param name="sessionId">Session id, which is used to track the execution status.</param>
    /// <param name="input">The input from client to continue the execution.</param>
    /// <param name="contextVariables">The request context variables </param>
    /// <returns>The execution context</returns>
    Task<ContextVariables> ExecuteAsync(Flow flow, string sessionId, string input, ContextVariables contextVariables);
}
