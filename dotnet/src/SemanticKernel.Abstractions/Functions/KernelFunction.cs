﻿// Copyright (c) Microsoft.All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Functions;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace Microsoft.SemanticKernel;

/// <summary>
/// Represents a function that can be invoked as part of a Semantic Kernel workload.
/// </summary>
public abstract class KernelFunction : FullyQualifiedAIFunction, IKernelFunction
{
    private static readonly JsonElement s_defaultSchema = JsonDocument.Parse("{}").RootElement;

    /// <summary>The measurement tag name for the function name.</summary>
    private protected const string MeasurementFunctionTagName = "semantic_kernel.function.name";

    /// <summary>The measurement tag name for the function error type.</summary>
    private protected const string MeasurementErrorTagName = "error.type";

    /// <summary><see cref="ActivitySource"/> for function-related activities.</summary>
    private static readonly ActivitySource s_activitySource = new("Microsoft.SemanticKernel");

    /// <summary><see cref="Meter"/> for function-related metrics.</summary>
    private protected static readonly Meter s_meter = new("Microsoft.SemanticKernel");

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization of various aspects of the function.</summary>
    private readonly JsonSerializerOptions? _jsonSerializerOptions;

    /// <summary>The underlying method, if this function was created from a method.</summary>
#pragma warning disable CA1051
    protected MethodInfo? _underlyingMethod;
#pragma warning restore CA1051

    /// <summary>The <see cref="Kernel"/> instance that will be prioritized when invoking without a provided <see cref="Kernel"/> argument.</summary>
    /// <remarks>This will be normally used when the function is invoked using the <see cref="AIFunction.InvokeAsync(AIFunctionArguments?, CancellationToken)"/> interface.</remarks>
    internal Kernel? Kernel { get; set; }

    /// <summary><see cref="Histogram{T}"/> to record function invocation duration.</summary>
    private static readonly Histogram<double> s_invocationDuration = s_meter.CreateHistogram<double>(
        "semantic_kernel.function.invocation.duration",
        "s",
        "Measures the duration of a function's execution");

    /// <summary><see cref="Histogram{T}"/> to record function streaming duration.</summary>
    /// <remarks>
    /// As this metric spans the full async iterator's lifecycle, it is inclusive of any time
    /// spent in the consuming code between MoveNextAsync calls on the enumerator.
    /// </remarks>
    private static readonly Histogram<double> s_streamingDuration = s_meter.CreateHistogram<double>(
        "semantic_kernel.function.streaming.duration",
        "s",
        "Measures the duration of a function's streaming execution");

    /// <summary>
    /// Gets the name of the function.
    /// </summary>
    /// <remarks>
    /// The name is used anywhere the function needs to be identified, such as in plans describing what functions
    /// should be invoked when, or as part of lookups in a plugin's function collection. Function names are generally
    /// handled in an ordinal case-insensitive manner.
    /// </remarks>\
    [JsonPropertyName("name")]
    [JsonInclude]
    public new virtual string Name => Metadata.Name;

    /// <summary>
    /// Gets the name of the plugin this function was added to.
    /// </summary>
    /// <remarks>
    /// The plugin name will be null if the function has not been added to a plugin.
    /// When a function is added to a plugin it will be cloned and the plugin name will be set.
    /// </remarks>
    public virtual string? PluginName => Metadata.PluginName;

    /// <summary>
    /// Gets a description of the function.
    /// </summary>
    /// <remarks>
    /// The description may be supplied to a model in order to elaborate on the function's purpose,
    /// in case it may be beneficial for the model to recommend invoking the function.
    /// </remarks>
    public override string Description => Metadata.Description;

    /// <summary>
    /// Gets the prompt execution settings.
    /// </summary>
    /// <remarks>
    /// The instances of <see cref="PromptExecutionSettings"/> are frozen and cannot be modified.
    /// </remarks>
    [JsonIgnore]
    public IReadOnlyDictionary<string, PromptExecutionSettings>? ExecutionSettings { get; }


    /// <summary>
    /// Initializes a new instance of the <see cref="KernelFunction"/> class.
    /// </summary>
    /// <param name="name">A name of the function to use as its <see cref="AITool.Name"/>.</param>
    /// <param name="description">The description of the function to use as its <see cref="AITool.Description"/>.</param>
    /// <param name="parameters">The metadata describing the parameters to the function.</param>
    /// <param name="returnParameter">The metadata describing the return parameter of the function.</param>
    /// <param name="executionSettings">
    /// The <see cref="PromptExecutionSettings"/> to use with the function. These will apply unless they've been
    /// overridden by settings passed into the invocation of the function.
    /// </param>
    [RequiresUnreferencedCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    [RequiresDynamicCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    protected KernelFunction(
        string name,
        string description,
        IReadOnlyList<KernelParameterMetadata> parameters,
        KernelReturnParameterMetadata? returnParameter = null,
        Dictionary<string, PromptExecutionSettings>? executionSettings = null)
        : this(name,
            null,
            description,
            parameters,
            returnParameter,
            executionSettings)
    {
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="KernelFunction"/> class.
    /// </summary>
    /// <param name="name">A name of the function to use as its <see cref="AITool.Name"/>.</param>
    /// <param name="description">The description of the function to use as its <see cref="AITool.Description"/>.</param>
    /// <param name="parameters">The metadata describing the parameters to the function.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization of various aspects of the function.</param>
    /// <param name="returnParameter">The metadata describing the return parameter of the function.</param>
    /// <param name="executionSettings">
    /// The <see cref="PromptExecutionSettings"/> to use with the function. These will apply unless they've been
    /// overridden by settings passed into the invocation of the function.
    /// </param>
    internal KernelFunction(
        string name,
        string description,
        IReadOnlyList<KernelParameterMetadata> parameters,
        JsonSerializerOptions jsonSerializerOptions,
        KernelReturnParameterMetadata? returnParameter = null,
        Dictionary<string, PromptExecutionSettings>? executionSettings = null)
        : this(name,
            null,
            description,
            parameters,
            jsonSerializerOptions,
            returnParameter,
            executionSettings)
    {
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="KernelFunction"/> class.
    /// </summary>
    /// <param name="name">A name of the function to use as its <see cref="AITool.Name"/>.</param>
    /// <param name="pluginName">The name of the plugin this function instance has been added to.</param>
    /// <param name="description">The description of the function to use as its <see cref="AITool.Description"/>.</param>
    /// <param name="parameters">The metadata describing the parameters to the function.</param>
    /// <param name="returnParameter">The metadata describing the return parameter of the function.</param>
    /// <param name="executionSettings">
    /// The <see cref="PromptExecutionSettings"/> to use with the function. These will apply unless they've been
    /// overridden by settings passed into the invocation of the function.
    /// </param>
    /// <param name="additionalMetadata">Properties/metadata associated with the function itself rather than its parameters and return type.</param>
    [RequiresUnreferencedCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    [RequiresDynamicCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    protected KernelFunction(
        string name,
        string? pluginName,
        string description,
        IReadOnlyList<KernelParameterMetadata> parameters,
        KernelReturnParameterMetadata? returnParameter = null,
        Dictionary<string, PromptExecutionSettings>? executionSettings = null,
        ReadOnlyDictionary<string, object?>? additionalMetadata = null)
        : base(new KernelFunctionMetadata(Throw.IfNull(name))
        {
            PluginName = pluginName,
            Description = description,
            Parameters = KernelVerify.ParametersUniqueness(parameters),
            ReturnParameter = returnParameter ?? KernelReturnParameterMetadata.Empty,
            AdditionalProperties = additionalMetadata ?? KernelFunctionMetadata.s_emptyDictionary
        })
    {
        BuildFunctionSchema();

        if (executionSettings is not null)
        {
            ExecutionSettings = executionSettings.ToDictionary(
                entry => entry.Key,
                entry =>
                {
                    PromptExecutionSettings clone = entry.Value.Clone();
                    clone.Freeze();

                    return clone;
                });
        }
    }


    /// <inheritdoc/>
    public override JsonElement JsonSchema => _jsonSchema;


    /// <summary>
    /// Initializes a new instance of the <see cref="KernelFunction"/> class.
    /// </summary>
    /// <param name="name">A name of the function to use as its <see cref="AITool.Name"/>.</param>
    /// <param name="pluginName">The name of the plugin this function instance has been added to.</param>
    /// <param name="description">The description of the function to use as its <see cref="AITool.Description"/>.</param>
    /// <param name="parameters">The metadata describing the parameters to the function.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization of various aspects of the function.</param>
    /// <param name="returnParameter">The metadata describing the return parameter of the function.</param>
    /// <param name="executionSettings">
    /// The <see cref="PromptExecutionSettings"/> to use with the function. These will apply unless they've been
    /// overridden by settings passed into the invocation of the function.
    /// </param>
    /// <param name="additionalMetadata">Properties/metadata associated with the function itself rather than its parameters and return type.</param>
    internal KernelFunction(
        string name,
        string? pluginName,
        string description,
        IReadOnlyList<KernelParameterMetadata> parameters,
        JsonSerializerOptions jsonSerializerOptions,
        KernelReturnParameterMetadata? returnParameter = null,
        Dictionary<string, PromptExecutionSettings>? executionSettings = null,
        ReadOnlyDictionary<string, object?>? additionalMetadata = null)
        : base(new KernelFunctionMetadata(Throw.IfNull(name))
        {
            PluginName = pluginName,
            Description = description,
            Parameters = KernelVerify.ParametersUniqueness(parameters),
            ReturnParameter = returnParameter ?? KernelReturnParameterMetadata.Empty,
            AdditionalProperties = additionalMetadata ?? KernelFunctionMetadata.s_emptyDictionary
        })
    {
        Verify.NotNull(jsonSerializerOptions);

        BuildFunctionSchema();

        if (executionSettings is not null)
        {
            ExecutionSettings = executionSettings.ToDictionary(
                entry => entry.Key,
                entry =>
                {
                    PromptExecutionSettings clone = entry.Value.Clone();
                    clone.Freeze();
                    return clone;
                });
        }

        _jsonSerializerOptions = jsonSerializerOptions;
    }


    /// <inheritdoc/>
    public override JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptions ?? base.JsonSerializerOptions;

    /// <inheritdoc/>
    public override MethodInfo? UnderlyingMethod => _underlyingMethod;


    /// <summary>
    /// Invokes the <see cref="KernelFunction"/>.
    /// </summary>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    /// <param name="arguments">The arguments to pass to the function's invocation, including any <see cref="PromptExecutionSettings"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The result of the function's execution.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="kernel"/> is null.</exception>
    public async Task<FunctionResult> InvokeAsync(
        Kernel kernel,
        KernelArguments? arguments = null,
        CancellationToken cancellationToken = default)
    {
        kernel ??= Kernel;
        Verify.NotNull(kernel);

        using Activity? activity = s_activitySource.StartFunctionActivity(Name, Description);
        ILogger logger = kernel.LoggerFactory.CreateLogger(typeof(KernelFunction));

        // Ensure arguments are initialized.
        arguments ??= [];
        logger.LogFunctionInvoking(PluginName, Name);

        LogFunctionArguments(logger,
            PluginName,
            Name,
            arguments);

        TagList tags = new() { { MeasurementFunctionTagName, Name } };
        long startingTimestamp = Stopwatch.GetTimestamp();
        FunctionResult functionResult = new(this, culture: kernel.Culture);

        try
        {
            // Quick check for cancellation after logging about function start but before doing any real work.
            cancellationToken.ThrowIfCancellationRequested();

            FunctionInvocationContext? invocationContext = await kernel.OnFunctionInvocationAsync(this,
                    arguments,
                    functionResult,
                    false,
                    async context =>
                    {
                        // Invoking the function and updating context with result.
                        context.Result = functionResult = await InvokeCoreAsync(kernel, context.Arguments, cancellationToken).ConfigureAwait(false);
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            // Apply any changes from the function filters context to final result.
            functionResult = invocationContext.Result;

            logger.LogFunctionInvokedSuccess(PluginName, Name);

            LogFunctionResult(logger,
                PluginName,
                Name,
                functionResult);

            return functionResult;
        }
        catch (Exception ex)
        {
            HandleException(ex,
                logger,
                activity,
                this,
                kernel,
                arguments,
                functionResult,
                ref tags);

            throw;
        }
        finally
        {
            // Record the invocation duration metric and log the completion.
            TimeSpan duration = new((long)((Stopwatch.GetTimestamp() - startingTimestamp) * (10_000_000.0 / Stopwatch.Frequency)));
            s_invocationDuration.Record(duration.TotalSeconds, in tags);
            logger.LogFunctionComplete(PluginName, Name, duration.TotalSeconds);
        }
    }


    /// <summary>
    /// Invokes the <see cref="KernelFunction"/>.
    /// </summary>
    /// <typeparam name="TResult">Specifies the type of the result value of the function.</typeparam>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    /// <param name="arguments">The arguments to pass to the function's invocation, including any <see cref="PromptExecutionSettings"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The result of the function's execution, cast to <typeparamref name="TResult"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="kernel"/> is null.</exception>
    /// <exception cref="InvalidCastException">The function's result could not be cast to <typeparamref name="TResult"/>.</exception>
    public async Task<TResult?> InvokeAsync<TResult>(
        Kernel kernel,
        KernelArguments? arguments = null,
        CancellationToken cancellationToken = default)
    {
        FunctionResult result = await InvokeAsync(kernel, arguments, cancellationToken).ConfigureAwait(false);

        return result.GetValue<TResult>();
    }


    /// <summary>
    /// Invokes the <see cref="KernelFunction"/> and streams its results.
    /// </summary>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    /// <param name="arguments">The arguments to pass to the function's invocation, including any <see cref="PromptExecutionSettings"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> for streaming the results of the function's invocation.</returns>
    /// <remarks>
    /// The function will not be invoked until an enumerator is retrieved from the returned <see cref="IAsyncEnumerable{T}"/>
    /// and its iteration initiated via an initial call to <see cref="IAsyncEnumerator{T}.MoveNextAsync"/>.
    /// </remarks>
    public IAsyncEnumerable<StreamingKernelContent> InvokeStreamingAsync(
        Kernel kernel,
        KernelArguments? arguments = null,
        CancellationToken cancellationToken = default)
    {
        return InvokeStreamingAsync<StreamingKernelContent>(kernel, arguments, cancellationToken);
    }


    /// <summary>
    /// Invokes the <see cref="KernelFunction"/> and streams its results.
    /// </summary>
    /// <typeparam name="TResult">Specifies the type of the result values of the function.</typeparam>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    /// <param name="arguments">The arguments to pass to the function's invocation, including any <see cref="PromptExecutionSettings"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>An <see cref="IAsyncEnumerable{TResult}"/> for streaming the results of the function's invocation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="kernel"/> is null.</exception>
    /// <remarks>
    /// The function will not be invoked until an enumerator is retrieved from the returned <see cref="IAsyncEnumerable{T}"/>
    /// and its iteration initiated via an initial call to <see cref="IAsyncEnumerator{T}.MoveNextAsync"/>.
    /// </remarks>
    public async IAsyncEnumerable<TResult> InvokeStreamingAsync<TResult>(
        Kernel kernel,
        KernelArguments? arguments = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        kernel ??= Kernel;
        Verify.NotNull(kernel);

        using Activity? activity = s_activitySource.StartFunctionActivity(Name, Description);
        ILogger logger = kernel.LoggerFactory.CreateLogger(Name) ?? NullLogger.Instance;

        arguments ??= [];
        logger.LogFunctionStreamingInvoking(PluginName, Name);

        LogFunctionArguments(logger,
            PluginName,
            Name,
            arguments);

        TagList tags = new() { { MeasurementFunctionTagName, Name } };
        long startingTimestamp = Stopwatch.GetTimestamp();

        try
        {
            IAsyncEnumerator<TResult> enumerator;

            try
            {
                // Quick check for cancellation after logging about function start but before doing any real work.
                cancellationToken.ThrowIfCancellationRequested();

                FunctionResult functionResult = new(this, culture: kernel.Culture);

                FunctionInvocationContext? invocationContext = await kernel.OnFunctionInvocationAsync(this,
                        arguments,
                        functionResult,
                        true,
                        context =>
                        {
                            // Invoke the function and get its streaming enumerable.
                            IAsyncEnumerable<TResult> enumerable = InvokeStreamingCoreAsync<TResult>(kernel, context.Arguments, cancellationToken);

                            // Update context with enumerable as result value.
                            context.Result = new FunctionResult(this, enumerable, kernel.Culture);

                            return Task.CompletedTask;
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                // Apply changes from the function filters to final result.
                IAsyncEnumerable<TResult> enumerable = invocationContext.Result.GetValue<IAsyncEnumerable<TResult>>() ?? AsyncEnumerable.Empty<TResult>();
                enumerator = enumerable.GetAsyncEnumerator(cancellationToken);

                // yielding within a try/catch isn't currently supported, so we break out of the try block
                // in order to then wrap the actual MoveNextAsync in its own try/catch and allow the yielding
                // to be lifted to be outside of the try/catch.
            }
            catch (Exception ex)
            {
                HandleException(ex,
                    logger,
                    activity,
                    this,
                    kernel,
                    arguments,
                    null,
                    ref tags);
                throw;
            }

            // Ensure we clean up after the enumerator.
            await using (enumerator.ConfigureAwait(false))
            {
                while (true)
                {
                    try
                    {
                        // Move to the next streaming result.
                        if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex,
                            logger,
                            activity,
                            this,
                            kernel,
                            arguments,
                            null,
                            ref tags);
                        throw;
                    }

                    // Yield the next streaming result.
                    yield return enumerator.Current;
                }
            }
        }
        finally
        {
            // Record the streaming duration metric and log the completion.
            TimeSpan duration = new((long)((Stopwatch.GetTimestamp() - startingTimestamp) * (10_000_000.0 / Stopwatch.Frequency)));
            s_streamingDuration.Record(duration.TotalSeconds, in tags);
            logger.LogFunctionStreamingComplete(PluginName, Name, duration.TotalSeconds);
        }
    }


    /// <summary>
    /// Creates a new <see cref="KernelFunction"/> object that is a copy of the current instance
    /// but the <see cref="KernelFunctionMetadata"/> has the plugin name set.
    /// </summary>
    /// <param name="pluginName">The optional name of the plugin this function instance will be added to.</param>
    /// <remarks>
    /// This method should only be used to create a new instance of a <see cref="KernelFunction"/> when adding
    /// a function to a <see cref="KernelPlugin"/>.
    /// </remarks>
    public abstract KernelFunction Clone(string? pluginName = null);


    /// <inheritdoc/>
    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(PluginName)
            ? Name
            : $"{PluginName}.{Name}";
    }


    /// <summary>
    /// Invokes the <see cref="KernelFunction"/>.
    /// </summary>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    /// <param name="arguments">The arguments to pass to the function's invocation, including any <see cref="PromptExecutionSettings"/>.</param>
    /// <returns>The updated context, potentially a new one if context switching is implemented.</returns>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    protected abstract ValueTask<FunctionResult> InvokeCoreAsync(
        Kernel kernel,
        KernelArguments arguments,
        CancellationToken cancellationToken);


    /// <summary>
    /// Invokes the <see cref="KernelFunction"/> using the <see cref="AIFunction"/> interface.
    /// </summary>
    /// <remarks>
    /// When using the <see cref="AIFunction.InvokeAsync"/> interface, the <see cref="Kernel"/> will be acquired as follows, in order of priority:
    /// <list type="number">
    /// <item>From the <see cref="AIFunctionArguments"/> dictionary with the <see cref="AIFunctionArgumentsExtensions.KernelAIFunctionArgumentKey"/> key.</item>
    /// <item>From the <see cref="AIFunctionArguments"/>.<see cref="AIFunctionArguments.Services"/> service provider.</item>
    /// <item>From the <see cref="Kernel"/> provided in <see cref="KernelFunctionExtensions.WithKernel"/> when Cloning the <see cref="KernelFunction"/>.</item>
    /// <item>A new <see cref="Kernel"/> instance will be created using the same service provider in the <see cref="AIFunctionArguments"/>.<see cref="AIFunctionArguments.Services"/>.</item>
    /// </list>
    /// </remarks>
    /// <param name="arguments">The arguments to pass to the function's invocation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The result of the function's execution.</returns>
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        Kernel kernel = arguments.TryGetValue(AIFunctionArgumentsExtensions.KernelAIFunctionArgumentKey, out object? kernelObject) && kernelObject is not null
            ? (kernelObject as Kernel)!
            : arguments.Services?.GetService(typeof(Kernel)) as Kernel
            ?? Kernel
            ?? new Kernel(arguments.Services);

        KernelArguments kernelArguments = new(arguments);

        FunctionResult? result = await InvokeCoreAsync(kernel, kernelArguments, cancellationToken).ConfigureAwait(false);

        // Serialize the result to JSON, as with AIFunctionFactory.Create AIFunctions.
        return result.Value is object value
            ? JsonSerializer.SerializeToElement(value, AbstractionsJsonContext.GetTypeInfo(value.GetType(), JsonSerializerOptions))
            : null;
    }


    /// <summary>
    /// Invokes the <see cref="KernelFunction"/> and streams its results.
    /// </summary>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    /// <param name="arguments">The arguments to pass to the function's invocation, including any <see cref="PromptExecutionSettings"/>.</param>
    /// <returns>The updated context, potentially a new one if context switching is implemented.</returns>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    protected abstract IAsyncEnumerable<TResult> InvokeStreamingCoreAsync<TResult>(
        Kernel kernel,
        KernelArguments arguments,
        CancellationToken cancellationToken);


    /// <summary>Handles special-cases for exception handling when invoking a function.</summary>
    private static void HandleException(
        Exception ex,
        ILogger logger,
        Activity? activity,
        KernelFunction kernelFunction,
        Kernel kernel,
        KernelArguments arguments,
        FunctionResult? result,
        ref TagList tags)
    {
        // Log the exception and add its type to the tags that'll be included with recording the invocation duration.
        tags.Add(MeasurementErrorTagName, ex.GetType().FullName);

        activity?.SetError(ex);
        logger.LogFunctionError(kernelFunction.PluginName,
            kernelFunction.Name,
            ex,
            ex.Message);

        // If the exception is an OperationCanceledException, wrap it in a KernelFunctionCanceledException
        // in order to convey additional details about what function was canceled. This is particularly
        // important for cancellation that occurs in response to the FunctionInvoked event, in which case
        // there may be a result from a successful function invocation, and we want that result to be
        // visible to a consumer if that's needed.
        if (ex is OperationCanceledException cancelEx)
        {
            KernelFunctionCanceledException kernelEx = new(kernel,
                kernelFunction,
                arguments,
                result,
                cancelEx);

            foreach (DictionaryEntry entry in cancelEx.Data)
            {
                kernelEx.Data.Add(entry.Key, entry.Value);
            }

            throw kernelEx;
        }
    }


    private void BuildFunctionSchema()
    {
        KernelFunctionSchemaModel schemaModel = new()
        {
            Type = "object",
            Description = Description
        };

        foreach (KernelParameterMetadata? parameter in Metadata.Parameters)
        {
            schemaModel.Properties[parameter.Name] = parameter.Schema?.RootElement ?? s_defaultSchema;

            if (parameter.IsRequired)
            {
                (schemaModel.Required ??= []).Add(parameter.Name);
            }
        }

        _jsonSchema = JsonSerializer.SerializeToElement(schemaModel, AbstractionsJsonContext.Default.KernelFunctionSchemaModel);
    }


    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "The warning is shown and should be addressed at the function creation site; there is no need to show it again at the function invocation sites.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "The warning is shown and should be addressed at the function creation site; there is no need to show it again at the function invocation sites.")]
    private void LogFunctionArguments(
        ILogger logger,
        string? pluginName,
        string functionName,
        KernelArguments arguments)
    {
        if (JsonSerializerOptions is not null)
        {
            logger.LogFunctionArguments(pluginName,
                functionName,
                arguments,
                JsonSerializerOptions);
        }
        else
        {
            logger.LogFunctionArguments(pluginName, functionName, arguments);
        }
    }


    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "The warning is shown and should be addressed at the function creation site; there is no need to show it again at the function invocation sites.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "The warning is shown and should be addressed at the function creation site; there is no need to show it again at the function invocation sites.")]
    private void LogFunctionResult(
        ILogger logger,
        string? pluginName,
        string functionName,
        FunctionResult functionResult)
    {
        if (JsonSerializerOptions is not null)
        {
            logger.LogFunctionResultValue(pluginName,
                functionName,
                functionResult,
                JsonSerializerOptions);
        }
        else
        {
            logger.LogFunctionResultValue(pluginName, functionName, functionResult);
        }
    }


    /// <summary>Creates an <see cref="AIFunction"/> for this <see cref="KernelFunction"/>.</summary>
    /// <param name="kernel">
    /// The <see cref="Kernel"/> instance to pass to the <see cref="KernelFunction"/> when it's invoked as part of the <see cref="AIFunction"/>'s invocation.
    /// </param>
    /// <returns>An instance of <see cref="AIFunction"/> that, when invoked, will in turn invoke the current <see cref="KernelFunction"/>.</returns>
    [Experimental("SKEXP0001")]
    [Obsolete("Use the kernel function directly or for similar behavior use Clone(Kernel) method instead.")]
    public AIFunction AsAIFunction(Kernel? kernel = null)
    {
        return new KernelAIFunction(this, kernel);
    }


    #region Private

    private JsonElement _jsonSchema;


    /// <summary>An <see cref="AIFunction"/> wrapper around a <see cref="KernelFunction"/>.</summary>
    [Obsolete("Use the kernel function directly or for similar behavior use Clone(Kernel) method instead.")]
    private sealed class KernelAIFunction : AIFunction
    {
        private static readonly JsonElement s_defaultSchema = JsonDocument.Parse("{}").RootElement;
        private readonly KernelFunction _kernelFunction;
        private readonly Kernel? _kernel;


        public KernelAIFunction(KernelFunction kernelFunction, Kernel? kernel)
        {
            _kernelFunction = kernelFunction;
            _kernel = kernel;
            Name = string.IsNullOrWhiteSpace(kernelFunction.PluginName)
                ? kernelFunction.Name
                : $"{kernelFunction.PluginName}_{kernelFunction.Name}";

            JsonSchema = BuildFunctionSchema(kernelFunction);
        }


        public override string Name { get; }
        public override JsonElement JsonSchema { get; }
        public override string Description => _kernelFunction.Description;
        public override JsonSerializerOptions JsonSerializerOptions => _kernelFunction.JsonSerializerOptions ?? base.JsonSerializerOptions;


        protected override async ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments? arguments = null,
            CancellationToken cancellationToken = default)
        {
            Verify.NotNull(arguments);

            // Create the KernelArguments from the supplied arguments.
            KernelArguments args = [];

            foreach (KeyValuePair<string, object?> argument in arguments)
            {
                args[argument.Key] = argument.Value;
            }

            // Invoke the KernelFunction.
            FunctionResult? functionResult = await _kernelFunction.InvokeAsync(_kernel ?? new Kernel(), args, cancellationToken).ConfigureAwait(false);

            // Serialize the result to JSON, as with AIFunctionFactory.Create AIFunctions.
            return functionResult.Value is object value
                ? JsonSerializer.SerializeToElement(value, AbstractionsJsonContext.GetTypeInfo(value.GetType(), _kernelFunction.JsonSerializerOptions))
                : null;
        }


        private static JsonElement BuildFunctionSchema(KernelFunction function)
        {
            KernelFunctionSchemaModel schemaModel = new()
            {
                Type = "object",
                Description = function.Description
            };

            foreach (KernelParameterMetadata? parameter in function.Metadata.Parameters)
            {
                schemaModel.Properties[parameter.Name] = parameter.Schema?.RootElement ?? s_defaultSchema;

                if (parameter.IsRequired)
                {
                    (schemaModel.Required ??= []).Add(parameter.Name);
                }
            }

            return JsonSerializer.SerializeToElement(schemaModel, AbstractionsJsonContext.Default.KernelFunctionSchemaModel);
        }
    }

    #endregion


}
