﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Microsoft.SemanticKernel.ChatCompletion;

/// <summary>Provides a <see cref="KernelFunction"/> that wraps an <see cref="AIFunction"/>.</summary>
internal sealed class AIFunctionKernelFunction : KernelFunction
{
    private readonly AIFunction _aiFunction;

    public AIFunctionKernelFunction(AIFunction aiFunction) :
        base(
            name: aiFunction.Name,
            description: aiFunction.Description,
            parameters: MapParameterMetadata(aiFunction),
            jsonSerializerOptions: aiFunction.JsonSerializerOptions,
            new KernelReturnParameterMetadata(AbstractionsJsonContext.Default.Options)
            {
                Description = aiFunction.UnderlyingMethod?.ReturnParameter.GetCustomAttribute<DescriptionAttribute>()?.Description,
                ParameterType = aiFunction.UnderlyingMethod?.ReturnParameter.ParameterType,
                Schema = new KernelJsonSchema(aiFunction.ReturnJsonSchema ?? AIJsonUtilities.CreateJsonSchema(aiFunction.UnderlyingMethod?.ReturnParameter.ParameterType)),
            })
    {
        // Kernel functions created from AI functions are always fully qualified
        _aiFunction = aiFunction;
    }

    private AIFunctionKernelFunction(AIFunctionKernelFunction other, string? pluginName) :
        base(other.Name, pluginName, other.Description, other.Metadata.Parameters, AbstractionsJsonContext.Default.Options, other.Metadata.ReturnParameter)
    {
        _aiFunction = other._aiFunction;
    }

    public override KernelFunction Clone(string? pluginName = null)
    {
        // Should allow null but not empty or whitespace
        if (pluginName is not null)
        {
            Verify.NotNullOrWhiteSpace(pluginName);
        }

        return new AIFunctionKernelFunction(this, pluginName);
    }

    protected override async ValueTask<FunctionResult> InvokeCoreAsync(
        Kernel kernel, KernelArguments arguments, CancellationToken cancellationToken)
    {
        if (_aiFunction is KernelFunction kernelFunction)
        {
            return await kernelFunction.InvokeAsync(kernel, arguments, cancellationToken).ConfigureAwait(false);
        }

        object? result = await _aiFunction.InvokeAsync(new(arguments), cancellationToken).ConfigureAwait(false);
        return new FunctionResult(this, result);
    }

    protected override async IAsyncEnumerable<TResult> InvokeStreamingCoreAsync<TResult>(
        Kernel kernel, KernelArguments arguments, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        object? result = await _aiFunction.InvokeAsync(new(arguments), cancellationToken).ConfigureAwait(false);
        yield return (TResult)result!;
    }

    private static IReadOnlyList<KernelParameterMetadata> MapParameterMetadata(AIFunction aiFunction)
    {
        if (aiFunction is KernelFunction kernelFunction)
        {
            return kernelFunction.Metadata.Parameters;
        }

        if (!aiFunction.JsonSchema.TryGetProperty("properties", out JsonElement properties))
        {
            return Array.Empty<KernelParameterMetadata>();
        }
        HashSet<string>? requiredParameters = GetRequiredParameterNames(aiFunction.JsonSchema);

        List<KernelParameterMetadata> kernelParams = [];
        var parameterInfos = aiFunction.UnderlyingMethod?.GetParameters().ToDictionary(p => p.Name!, StringComparer.Ordinal);
        foreach (var param in properties.EnumerateObject())
        {
            ParameterInfo? paramInfo = null;
            parameterInfos?.TryGetValue(param.Name, out paramInfo);
            kernelParams.Add(new(param.Name, aiFunction.JsonSerializerOptions)
            {
                Description = param.Value.TryGetProperty("description", out JsonElement description) ? description.GetString() : null,
                DefaultValue = param.Value.TryGetProperty("default", out JsonElement defaultValue) ? defaultValue : null,
                IsRequired = requiredParameters?.Contains(param.Name) ?? false,
                ParameterType = paramInfo?.ParameterType,
                Schema = param.Value.TryGetProperty("schema", out JsonElement schema)
                    ? new KernelJsonSchema(schema)
                    : new KernelJsonSchema(param.Value),
            });
        }

        return kernelParams;
    }

    /// <summary>
    /// Gets the names of the required parameters from the AI function's JSON schema.
    /// </summary>
    /// <param name="schema">The JSON schema of the AI function.</param>
    /// <returns>The names of the required parameters.</returns>
    private static HashSet<string>? GetRequiredParameterNames(JsonElement schema)
    {
        HashSet<string>? requiredParameterNames = null;

        if (schema.TryGetProperty("required", out JsonElement requiredElement) && requiredElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var node in requiredElement.EnumerateArray())
            {
                requiredParameterNames ??= [];
                requiredParameterNames.Add(node.GetString()!);
            }
        }

        return requiredParameterNames;
    }
}
