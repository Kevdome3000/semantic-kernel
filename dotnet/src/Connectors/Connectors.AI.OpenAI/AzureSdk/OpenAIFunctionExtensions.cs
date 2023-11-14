// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;

using System.Collections.Generic;


/// <summary>
/// Extensions for <see cref="FunctionView"/> specific to the OpenAI connector.
/// </summary>
internal static class OpenAIFunctionExtensions
{
    /// <summary>
    /// Convert a <see cref="OpenAIFunction"/> to an <see cref="FunctionView"/>.
    /// </summary>
    /// <param name="function">The <see cref="OpenAIFunction"/> object to convert.</param>
    /// <returns>An <see cref="FunctionView"/> object.</returns>
    public static FunctionView ToFunctionView(this OpenAIFunction function)
    {
        List<ParameterView> parameterViews = new List<ParameterView>();

        foreach (var openAIparameter in function.Parameters)
        {
            parameterViews.Add(new ParameterView(
                Name: openAIparameter.Name,
                Description: openAIparameter.Description,
                IsRequired: openAIparameter.IsRequired,
                Schema: openAIparameter.Schema,
                ParameterType: openAIparameter.ParameterType));
        }

        var returnParameter = new ReturnParameterView(
            Description: function.ReturnParameter.Description,
            Schema: function.ReturnParameter.Schema,
            ParameterType: function.ReturnParameter.ParameterType);

        return new FunctionView(
            function.FunctionName,
            function.PluginName,
            function.Description,
            parameterViews,
            returnParameter);
    }
}
