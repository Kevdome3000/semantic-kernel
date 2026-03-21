// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Amazon.BedrockAgent;
using Amazon.BedrockAgent.Model;
using Amazon.BedrockAgentRuntime.Model;
using FunctionSchema = Amazon.BedrockAgent.Model.FunctionSchema;
using ParameterDetail = Amazon.BedrockAgent.Model.ParameterDetail;
using Type = Amazon.BedrockAgent.Type;

namespace Microsoft.SemanticKernel.Agents.Bedrock;

/// <summary>
/// Extensions associated with the status of a <see cref="BedrockAgent"/>.
/// </summary>
internal static class BedrockFunctionSchemaExtensions
{
    public static KernelArguments FromFunctionParameters(this List<FunctionParameter> parameters, KernelArguments? arguments)
    {
        KernelArguments kernelArguments = arguments ?? [];

        foreach (var parameter in parameters)
        {
            kernelArguments.Add(parameter.Name, parameter.Value);
        }

        return kernelArguments;
    }


    public static FunctionSchema ToFunctionSchema(this Kernel kernel)
    {
        var plugins = kernel.Plugins;
        List<Function> functions = [];

        foreach (var plugin in plugins)
        {
            foreach (KernelFunction function in plugin)
            {
                functions.Add(new Function
                {
                    Name = function.Name,
                    Description = function.Description,
                    Parameters = function.Metadata.Parameters.CreateParameterSpec(),
                    // This field controls whether user confirmation is required to invoke the function.
                    // If this is set to "ENABLED", the user will be prompted to confirm the function invocation.
                    // Only after the user confirms, the function call request will be issued by the agent.
                    // If the user denies the confirmation, the agent will act as if the function does not exist.
                    // Currently, we do not support this feature, so we set it to "DISABLED".
                    RequireConfirmation = RequireConfirmation.DISABLED
                });
            }
        }

        return new FunctionSchema
        {
            Functions = functions
        };
    }


    private static Dictionary<string, ParameterDetail> CreateParameterSpec(
        this IReadOnlyList<KernelParameterMetadata> parameters)
    {
        Dictionary<string, ParameterDetail> parameterSpec = [];

        foreach (var parameter in parameters)
        {
            parameterSpec.Add(parameter.Name,
                new ParameterDetail
                {
                    Description = parameter.Description,
                    Required = parameter.IsRequired,
                    Type = parameter.ParameterType.ToAmazonType()
                });
        }

        return parameterSpec;
    }


    private static Type ToAmazonType(this System.Type? parameterType)
    {
        var typeString = parameterType?.GetFriendlyTypeName();
        return typeString switch
        {
            "String" => Type.String,
            "Boolean" => Type.Boolean,
            "Int16" => Type.Integer,
            "UInt16" => Type.Integer,
            "Int32" => Type.Integer,
            "UInt32" => Type.Integer,
            "Int64" => Type.Integer,
            "UInt64" => Type.Integer,
            "Single" => Type.Number,
            "Double" => Type.Number,
            "Decimal" => Type.Number,
            "String[]" => Type.Array,
            "Boolean[]" => Type.Array,
            "Int16[]" => Type.Array,
            "UInt16[]" => Type.Array,
            "Int32[]" => Type.Array,
            "UInt32[]" => Type.Array,
            "Int64[]" => Type.Array,
            "UInt64[]" => Type.Array,
            "Single[]" => Type.Array,
            "Double[]" => Type.Array,
            "Decimal[]" => Type.Array,
            _ => throw new ArgumentException($"Unsupported parameter type: {typeString}")
        };
    }
}
