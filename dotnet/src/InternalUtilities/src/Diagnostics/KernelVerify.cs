// Copyright (c) Microsoft. All rights reserved.

using System.Text.RegularExpressions;

namespace Microsoft.SemanticKernel;

[ExcludeFromCodeCoverage]
internal static partial class KernelVerify
{
    [GeneratedRegex("^[0-9A-Za-z_]*$")]
    private static partial Regex AllowedPluginNameSymbolsRegex();


    [GeneratedRegex("^[0-9A-Za-z_-]*$")]
    private static partial Regex AllowedFunctionNameSymbolsRegex();


    internal static void ValidPluginName([NotNull] string? pluginName, IReadOnlyKernelPluginCollection? plugins = null, [CallerArgumentExpression(nameof(pluginName))] string? paramName = null)
    {
        Verify.NotNullOrWhiteSpace(pluginName);

        if (!AllowedPluginNameSymbolsRegex().IsMatch(pluginName))
        {
            Verify.ThrowArgumentInvalidName("plugin name", pluginName, paramName);
        }

        if (plugins is not null && plugins.Contains(pluginName))
        {
            throw new ArgumentException($"A plugin with the name '{pluginName}' already exists.");
        }
    }


    internal static void ValidFunctionName([NotNull] string? functionName, [CallerArgumentExpression(nameof(functionName))] string? paramName = null)
    {
        Verify.NotNullOrWhiteSpace(functionName);

        if (!AllowedFunctionNameSymbolsRegex().IsMatch(functionName))
        {
            Verify.ThrowArgumentInvalidName("function name", functionName, paramName);
        }
    }


    /// <summary>
    /// Make sure every function parameter name is unique
    /// </summary>
    /// <param name="parameters">List of parameters</param>
    internal static IReadOnlyList<KernelParameterMetadata> ParametersUniqueness(IReadOnlyList<KernelParameterMetadata> parameters)
    {
        int count = parameters.Count;

        if (count > 0)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < count; i++)
            {
                KernelParameterMetadata p = parameters[i];

                if (string.IsNullOrWhiteSpace(p.Name))
                {
                    string paramName = $"{nameof(parameters)}[{i}].{p.Name}";

                    if (p.Name is null)
                    {
                        Verify.ThrowArgumentNullException(paramName);
                    }
                    else
                    {
                        Verify.ThrowArgumentWhiteSpaceException(paramName);
                    }
                }

                if (!seen.Add(p.Name))
                {
                    throw new ArgumentException($"The function has two or more parameters with the same name '{p.Name}'");
                }
            }
        }

        return parameters;
    }
}
