// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.FunctionCalling.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Orchestration;


/// <summary>
///  Extensions for converting between FunctionDefinition and FunctionView
/// </summary>
public static class FunctionExtensions
{

    /// <summary>
    /// Default FunctionDefinition
    /// </summary>
    public static readonly FunctionDefinition Default = new()
    {
        Name = "function_call",
        Description = "make a function call",
        Parameters = BinaryData.FromObjectAsJson(
            new
            {
                Type = "object",
                Properties = new
                {
                    FunctionCall = new
                    {
                        Type = "object",
                        Description = "Function call data structure",
                        Properties = new
                        {
                            Function = new
                            {
                                Type = "string",
                                Description = "Name of the function chosen"
                            },
                            Parameters = new
                            {
                                Type = "array",
                                Description = "Parameter values",
                                Items = new
                                {
                                    Type = "object",
                                    Description = "Parameter value",
                                    Properties = new
                                    {
                                        Name = new
                                        {
                                            Type = "string",
                                            Description = "Parameter name"
                                        },
                                        Value = new
                                        {
                                            Type = "string",
                                            Description = "Parameter value"
                                        }
                                    }
                                }
                            }
                        },
                        Required = new[] { "function", "parameters" }
                    }
                },
                Required = new[] { "functionCall" }

            }, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
    };


    /// <summary>
    ///  Convert FunctionView to FunctionDefinition
    /// </summary>
    /// <param name="functionView"></param>
    /// <returns></returns>
    public static FunctionDefinition ToFunctionDefinition(this FunctionView functionView)
    {
        // Convert Parameters 
        Dictionary<string, object> parameterProps = new();

        foreach (var param in functionView.Parameters)
        {
            string descriptionString = param.Description!;
            string defaultValue = param.DefaultValue ?? string.Empty;

            if (!string.IsNullOrEmpty(defaultValue))
            {
                descriptionString += $" (default={defaultValue})";
            }
            parameterProps[param.Name] = new
            {
                type = param.Type?.Name ?? "string",
                description = descriptionString
            };
        }

        // Form parameters as BinaryData
        var parameters = BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = parameterProps
        }, new JsonSerializerOptions()
            { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Console.WriteLine($"Parameters: {JsonSerializer.Deserialize()}");
        var functionName = string.IsNullOrEmpty(functionView.PluginName)
            ? functionView.Name
            : $"{functionView.PluginName}.{functionView.Name}";
        // Create FunctionDefinition
        return new FunctionDefinition()
        {
            Name = functionName.Replace('.', '_'),
            Description = functionView.Description,
            Parameters = parameters
        };
    }


    /// <summary>
    /// Check if the function can be called
    /// </summary>
    /// <param name="functionView"></param>
    /// <returns></returns>
    /// <remarks>
    ///  A function definition must have a description to be called
    /// </remarks>
    public static bool CanBeCalled(this FunctionView functionView) => !string.IsNullOrEmpty(functionView.Description);


    /// <summary>
    ///  Convert FunctionsView to FunctionDefinitions
    /// </summary>
    /// <param name="functionsView"></param>
    /// <returns></returns>
    public static IEnumerable<FunctionDefinition> ToFunctionDefinitions(this IEnumerable<FunctionView> functionsView)
    {
        return functionsView.Where(view => view.CanBeCalled()).Select(functionView => functionView.ToFunctionDefinition());
    }


    /// <summary>
    /// Get the FunctionDefinitions for the eligible functions in the skill collection
    /// </summary>
    /// <param name="skillCollection"></param>
    /// <param name="excludedSkills"></param>
    /// <returns></returns>
    public static IEnumerable<FunctionDefinition> GetFunctionDefinitions(this IReadOnlyFunctionCollection skillCollection, IEnumerable<string>? excludedSkills = null, IEnumerable<string>? excludedFunctions = null)
    {
        IReadOnlyList<FunctionView> functionsView = skillCollection.GetFunctionViews();

        excludedSkills ??= Array.Empty<string>();
        excludedFunctions ??= Array.Empty<string>();

        List<FunctionView> availableFunctions = functionsView
            .Where(s => !excludedSkills.Contains(s.PluginName) && !excludedFunctions.Contains(s.Name))
            .OrderBy(x => x.PluginName)
            .ThenBy(x => x.Name)
            .ToList();

        return availableFunctions.Where(view => view.CanBeCalled()).Select(functionView => functionView.ToFunctionDefinition());
    }


    /// <summary>
    ///  Get the Function for the FunctionCall
    /// </summary>
    /// <param name="skillCollection"></param>
    /// <param name="functionCall"></param>
    /// <param name="functionInstance"></param>
    /// <returns></returns>
    public static bool TryGetFunction(this IReadOnlyFunctionCollection skillCollection, FunctionCallResult functionCall, out ISKFunction? functionInstance)
    {

        // handles edge case where function name is prefixed with "functions." 
        if (functionCall.Function.StartsWith("functions.", StringComparison.Ordinal))
        {
            functionCall.Function = functionCall.Function.Replace("functions.", string.Empty).TrimStart();
        }

        if (skillCollection.TryGetFunction(functionCall.Function, out functionInstance))
        {
            return true;
        }

        // If the function name is not found, try to find it in the skill collection by splitting the function name into skill name and function name
        // cannot use '.' due to OpenAI request requirements -> '^[a-zA-Z0-9_-]{1,64}$'
        if (!functionCall.Function.Contains('_'))
        {
            return false;
        }

        var split = functionCall.Function.Split('_');
        var skillName = split[0];
        var functionName = split[1];

        return skillCollection.TryGetFunction(skillName, functionName, out functionInstance);
    }


    /// <summary>
    ///  Returns the ContextVariables for the FunctionCall
    /// </summary>
    /// <param name="functionCall"></param>
    /// <returns></returns>
    public static ContextVariables FunctionParameters(this FunctionCallResult functionCall)
    {
        var contextVariables = new ContextVariables();

        foreach (var parameter in functionCall.Parameters)
        {
            contextVariables[parameter.Name] = parameter.Value;
        }

        return contextVariables;
    }


    /// <summary>
    /// Converts the SKContext.Result to a FunctionCallResult
    /// </summary>
    /// <param name="context"></param>
    /// <param name="options"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? ToFunctionCallResult<T>(this SKContext context, JsonSerializerOptions? options = null)
    {
        T? result = default;
        ILogger logger = context.LoggerFactory.CreateLogger("FunctionCallResult");
        try
        {
            using var document = JsonDocument.Parse(context.Result.Trim());

            var root = document.RootElement;

            var propertyEnumerator = root.EnumerateObject();

            if (propertyEnumerator.MoveNext())
            {
                var firstProperty = propertyEnumerator.Current.Value;
                var firstElementJsonString = firstProperty.GetRawText();

                result = JsonSerializer.Deserialize<T>(firstElementJsonString, options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
            }

        }
        catch (JsonException ex)
        {
            logger.LogError("Error while converting \'{ContextResult}\' to a \'{Unknown}\': {Ex}", context.Result, typeof(T), ex);
            // Console.WriteLine($"Error while converting '{context.Result}' to a '{typeof(T)}': {ex}");
        }

        return result;
    }

}