// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.FunctionCalling;

using System;
using System.Text.Json.Serialization;


/// <summary>
///  A parameter for a function call
/// </summary>
public sealed class FunctionCallParameter
{
    /// <summary>
    ///  Constructor
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    [JsonConstructor]
    public FunctionCallParameter(string name, string value)
    {
        Name = name;
        Value = value;
    }


    /// <summary>
    ///  Name of the parameter
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; }

    /// <summary>
    ///  Value of the parameter
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; init; }


    /// <summary>
    ///  Compare two FunctionCallParameters
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if (obj is not FunctionCallParameter other)
        {
            return false;
        }
        var nameEquality = Name.Trim().Equals(other.Name.Trim(), StringComparison.Ordinal);
        var valueEquality = Value.Trim().Equals(other.Value.Trim(), StringComparison.Ordinal);
        return nameEquality && valueEquality;

    }

    /// <summary>
    ///  Get a hash code for the FunctionCallParameter
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() => HashCode.Combine(Name, Value);
}
