﻿// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130
namespace Microsoft.SemanticKernel;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;


/// <summary>
/// Represents the result of a <see cref="KernelFunction"/> invocation.
/// </summary>
public sealed class FunctionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionResult"/> class.
    /// </summary>
    /// <param name="function">The <see cref="KernelFunction"/> whose result is represented by this instance.</param>
    /// <param name="value">The resulting object of the function's invocation.</param>
    /// <param name="culture">The culture configured on the <see cref="Kernel"/> that executed the function.</param>
    /// <param name="metadata">Metadata associated with the function's execution</param>
    public FunctionResult(KernelFunction function, object? value = null, CultureInfo? culture = null, IDictionary<string, object?>? metadata = null)
    {
        Verify.NotNull(function);

        this.Function = function;
        this.Value = value;
        this.Culture = culture ?? CultureInfo.InvariantCulture;
        this.Metadata = metadata;
    }


    /// <summary>
    /// Gets the <see cref="KernelFunction"/> whose result is represented by this instance.
    /// </summary>
    public KernelFunction Function { get; }

    /// <summary>
    /// Gets any metadata associated with the function's execution.
    /// </summary>
    public IDictionary<string, object?>? Metadata { get; }

    /// <summary>
    /// Gets the <see cref="Type"/> of the function's result.
    /// </summary>
    /// <remarks>
    /// This or a base type is the type expected to be passed as the generic
    /// argument to <see cref="GetValue{T}"/>.
    /// </remarks>
    public Type? ValueType => this.Value?.GetType();


    /// <summary>
    /// Returns function result value.
    /// </summary>
    /// <typeparam name="T">Target type for result value casting.</typeparam>
    /// <exception cref="InvalidCastException">Thrown when it's not possible to cast result value to <typeparamref name="T"/>.</exception>
    public T? GetValue<T>()
    {
        if (this.Value is null)
        {
            return default;
        }

        if (this.Value is T typedResult)
        {
            return typedResult;
        }

        throw new InvalidCastException($"Cannot cast {this.Value.GetType()} to {typeof(T)}");
    }


    /// <inheritdoc/>
    public override string ToString() =>
        ConvertToString(this.Value, this.Culture) ?? string.Empty;


    /// <summary>
    /// Function result object.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// The culture configured on the Kernel that executed the function.
    /// </summary>

    internal CultureInfo Culture { get; }


    private static string? ConvertToString(object? value, CultureInfo culture)
    {
        if (value == null) { return null; }

        var sourceType = value.GetType();

        var converterFunction = GetTypeConverterDelegate(sourceType);

        return converterFunction == null
            ? value.ToString()
            : converterFunction(value, culture);
    }


    private static Func<object?, CultureInfo, string?>? GetTypeConverterDelegate(Type sourceType) =>
        s_converters.GetOrAdd(sourceType, static sourceType =>
        {
            // Strings just render as themselves.
            if (sourceType == typeof(string))
            {
                return (input, cultureInfo) => (string)input!;
            }

            // Look up and use a type converter.
            if (TypeConverterFactory.GetTypeConverter(sourceType) is TypeConverter converter && converter.CanConvertTo(typeof(string)))
            {
                return (input, cultureInfo) =>
                {
                    return converter.ConvertToString(context: null, cultureInfo, input);
                };
            }

            return null;
        });


    /// <summary>Converter functions for converting types to strings.</summary>
    private static readonly ConcurrentDictionary<Type, Func<object?, CultureInfo, string?>?> s_converters = new();
}