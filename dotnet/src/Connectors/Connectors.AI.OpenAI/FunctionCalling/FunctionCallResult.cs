// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.FunctionCalling;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;


/// <summary>
///  The function call to be made returned by the LLM
/// </summary>
public class FunctionCallResult
{

    /// <summary>
    /// Name of the function chosen
    /// </summary>
    [JsonPropertyName("function")]
    public string? Function { get; set; }

    /// <summary>
    ///  Parameter values
    /// </summary>
    [JsonPropertyName("parameters")]
    public List<FunctionCallParameter> Parameters { get; set; } = new();


    /// <summary>
    ///  Compare two FunctionCallResults
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if (obj is not FunctionCallResult otherFunctionCallResult)
        {
            return false;
        }
        // You might need to adjust this comparison depending on what makes two FunctionCallResult equal in your context
        bool functionEquality = otherFunctionCallResult.Function != null && otherFunctionCallResult.Function.Trim().Equals(Function?.Trim(), System.StringComparison.Ordinal);
        bool parametersEquality = otherFunctionCallResult.Parameters.SequenceEqual(Parameters);
        return functionEquality && parametersEquality;
    }


    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Function);
        foreach (var parameter in Parameters)
        {
            hashCode.Add(parameter);
        }
        return hashCode.ToHashCode();
    }

}
