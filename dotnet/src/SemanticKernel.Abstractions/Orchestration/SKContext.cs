// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Orchestration;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Diagnostics;
using Extensions.Logging;
using Extensions.Logging.Abstractions;
using Services;


/// <summary>
/// Semantic Kernel context.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class SKContext
{
    /// <summary>
    /// Print the processed input, aka the current data after any processing occurred.
    /// </summary>
    /// <returns>Processed input, aka result</returns>
    public string Result => Variables.ToString();

    /// <summary>
    /// When a prompt is processed, aka the current data after any model results processing occurred.
    /// (One prompt can have multiple results).
    /// </summary>
    [Obsolete($"ModelResults are now part of {nameof(FunctionResult.Metadata)} property. Use 'ModelResults' key or available extension methods to get model results.")]
    public IReadOnlyCollection<ModelResult> ModelResults => Array.Empty<ModelResult>();

    /// <summary>
    /// The culture currently associated with this context.
    /// </summary>
    public CultureInfo Culture
    {
        get => _culture;
        set => _culture = value ?? CultureInfo.CurrentCulture;
    }

    /// <summary>
    /// User variables
    /// </summary>
    public ContextVariables Variables { get; }

    /// <summary>
    /// Read only functions collection
    /// </summary>
    public IReadOnlyFunctionCollection Functions { get; }

    /// <summary>
    /// App logger
    /// </summary>
    public ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Executes functions using the current resources loaded in the context
    /// </summary>
    public IFunctionRunner Runner { get; }

    /// <summary>
    /// AI service provider
    /// </summary>
    public IAIServiceProvider ServiceProvider { get; }

    /// <summary>
    /// AIService selector implementation
    /// </summary>
    internal IAIServiceSelector ServiceSelector { get; }


    /// <summary>
    /// Constructor for the context.
    /// </summary>
    /// <param name="functionRunner">Function runner reference</param>
    /// <param name="serviceProvider">AI service provider</param>
    /// <param name="serviceSelector">AI service selector</param>
    /// <param name="variables">Context variables to include in context.</param>
    /// <param name="functions">Functions to include in context.</param>
    /// <param name="loggerFactory">Logger factory to be used in context</param>
    /// <param name="culture">Culture related to the context</param>
    internal SKContext(
        IFunctionRunner functionRunner,
        IAIServiceProvider serviceProvider,
        IAIServiceSelector serviceSelector,
        ContextVariables? variables = null,
        IReadOnlyFunctionCollection? functions = null,
        ILoggerFactory? loggerFactory = null,
        CultureInfo? culture = null)
    {
        Verify.NotNull(functionRunner, nameof(functionRunner));

        Runner = functionRunner;
        ServiceProvider = serviceProvider;
        ServiceSelector = serviceSelector;
        Variables = variables ?? new();
        Functions = functions ?? NullReadOnlyFunctionCollection.Instance;
        LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _culture = culture ?? CultureInfo.CurrentCulture;
    }


    /// <summary>
    /// Print the processed input, aka the current data after any processing occurred.
    /// </summary>
    /// <returns>Processed input, aka result.</returns>
    public override string ToString()
    {
        return Result;
    }


    /// <summary>
    /// Create a clone of the current context, using the same kernel references (memory, functions, logger)
    /// and a new set variables, so that variables can be modified without affecting the original context.
    /// </summary>
    /// <returns>A new context cloned from the current one</returns>
    public SKContext Clone()
        => Clone(null, null);


    /// <summary>
    /// Create a clone of the current context, using the same kernel references (memory, functions, logger)
    /// and optionally allows overriding the variables and functions.
    /// </summary>
    /// <param name="variables">Override the variables with the provided ones</param>
    /// <param name="functions">Override the functions with the provided ones</param>
    /// <returns>A new context cloned from the current one</returns>
    public SKContext Clone(ContextVariables? variables, IReadOnlyFunctionCollection? functions)
    {
        return new SKContext(
            Runner,
            this.ServiceProvider,
            this.ServiceSelector,
            variables ?? Variables.Clone(),
            functions ?? Functions,
            LoggerFactory,
            Culture);
    }


    /// <summary>
    /// The culture currently associated with this context.
    /// </summary>
    private CultureInfo _culture;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            string display = Variables.DebuggerDisplay;

            if (Functions is IReadOnlyFunctionCollection functions)
            {
                var view = functions.GetFunctionViews();
                display += $", Functions = {view.Count}";
            }

            display += $", Culture = {Culture.EnglishName}";

            return display;
        }
    }
}
