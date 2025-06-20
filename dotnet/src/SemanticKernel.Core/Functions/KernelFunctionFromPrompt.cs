﻿// Copyright (c) Microsoft.All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.TextGeneration;

namespace Microsoft.SemanticKernel;
/// <summary>
/// A Semantic Kernel "Semantic" prompt function.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal sealed class KernelFunctionFromPrompt : KernelFunction
{
    /// <summary>
    /// Creates a <see cref="KernelFunction"/> instance for a prompt specified via a prompt template.
    /// </summary>
    /// <param name="promptTemplate">Prompt template for the function, defined using the <see cref="PromptTemplateConfig.SemanticKernelTemplateFormat"/> template format.</param>
    /// <param name="executionSettings">Default execution settings to use when invoking this prompt function.</param>
    /// <param name="functionName">A name for the given function. The name can be referenced in templates and used by the pipeline planner.</param>
    /// <param name="description">The description to use for the function.</param>
    /// <param name="templateFormat">Optional format of the template. Must be provided if a prompt template factory is provided</param>
    /// <param name="promptTemplateFactory">Optional: Prompt template factory</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <returns>A function ready to use</returns>
    [RequiresUnreferencedCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    [RequiresDynamicCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    public static KernelFunction Create(
        string promptTemplate,
        Dictionary<string, PromptExecutionSettings>? executionSettings = null,
        string? functionName = null,
        string? description = null,
        string? templateFormat = null,
        IPromptTemplateFactory? promptTemplateFactory = null,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNullOrWhiteSpace(promptTemplate);

        if (promptTemplateFactory is not null)
        {
            if (string.IsNullOrWhiteSpace(templateFormat))
            {
                throw new ArgumentException($@"Template format is required when providing a {nameof(promptTemplateFactory)}", nameof(templateFormat));
            }
        }

        var promptConfig = new PromptTemplateConfig
        {
            TemplateFormat = templateFormat ?? PromptTemplateConfig.SemanticKernelTemplateFormat,
            Name = functionName,
            Description = description ?? "Generic function, unknown purpose",
            Template = promptTemplate
        };

        if (executionSettings is not null)
        {
            promptConfig.ExecutionSettings = executionSettings;
        }

        var factory = promptTemplateFactory ?? new KernelPromptTemplateFactory(loggerFactory);

        return Create(
            promptTemplate: factory.Create(promptConfig),
            promptConfig: promptConfig,
            loggerFactory: loggerFactory);
    }

    /// <summary>
    /// Creates a <see cref="KernelFunction"/> instance for a prompt specified via a prompt template.
    /// </summary>
    /// <param name="promptTemplate">Prompt template for the function, defined using the <see cref="PromptTemplateConfig.SemanticKernelTemplateFormat"/> template format.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization of various aspects of the function.</param>
    /// <param name="executionSettings">Default execution settings to use when invoking this prompt function.</param>
    /// <param name="functionName">A name for the given function. The name can be referenced in templates and used by the pipeline planner.</param>
    /// <param name="description">The description to use for the function.</param>
    /// <param name="templateFormat">Optional format of the template. Must be provided if a prompt template factory is provided</param>
    /// <param name="promptTemplateFactory">Optional: Prompt template factory</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <returns>A function ready to use</returns>
    public static KernelFunction Create(
        string promptTemplate,
        JsonSerializerOptions jsonSerializerOptions,
        Dictionary<string, PromptExecutionSettings>? executionSettings = null,
        string? functionName = null,
        string? description = null,
        string? templateFormat = null,
        IPromptTemplateFactory? promptTemplateFactory = null,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNullOrWhiteSpace(promptTemplate);

        if (promptTemplateFactory is not null)
        {
            if (string.IsNullOrWhiteSpace(templateFormat))
            {
                throw new ArgumentException($@"Template format is required when providing a {nameof(promptTemplateFactory)}", nameof(templateFormat));
            }
        }

        var promptConfig = new PromptTemplateConfig
        {
            TemplateFormat = templateFormat ?? PromptTemplateConfig.SemanticKernelTemplateFormat,
            Name = functionName,
            Description = description ?? "Generic function, unknown purpose",
            Template = promptTemplate
        };

        if (executionSettings is not null)
        {
            promptConfig.ExecutionSettings = executionSettings;
        }

        var factory = promptTemplateFactory ?? new KernelPromptTemplateFactory(loggerFactory);

        return Create(
            promptTemplate: factory.Create(promptConfig),
            promptConfig: promptConfig,
            jsonSerializerOptions: jsonSerializerOptions,
            loggerFactory: loggerFactory);
    }

    /// <summary>
    /// Creates a <see cref="KernelFunction"/> instance for a prompt specified via a prompt template configuration.
    /// </summary>
    /// <param name="promptConfig">Prompt template configuration</param>
    /// <param name="promptTemplateFactory">Optional: Prompt template factory</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <returns>A function ready to use</returns>
    [RequiresUnreferencedCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    [RequiresDynamicCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    public static KernelFunction Create(
        PromptTemplateConfig promptConfig,
        IPromptTemplateFactory? promptTemplateFactory = null,
        ILoggerFactory? loggerFactory = null)
    {
        var factory = promptTemplateFactory ?? new KernelPromptTemplateFactory(loggerFactory);

        return Create(
            promptTemplate: factory.Create(promptConfig),
            promptConfig: promptConfig,
            loggerFactory: loggerFactory);
    }

    /// <summary>
    /// Creates a <see cref="KernelFunction"/> instance for a prompt specified via a prompt template configuration.
    /// </summary>
    /// <param name="promptConfig">Prompt template configuration</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization of various aspects of the function.</param>
    /// <param name="promptTemplateFactory">Optional: Prompt template factory</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <returns>A function ready to use</returns>
    public static KernelFunction Create(
        PromptTemplateConfig promptConfig,
        JsonSerializerOptions jsonSerializerOptions,
        IPromptTemplateFactory? promptTemplateFactory = null,
        ILoggerFactory? loggerFactory = null)
    {
        var factory = promptTemplateFactory ?? new KernelPromptTemplateFactory(loggerFactory);

        return Create(
            promptTemplate: factory.Create(promptConfig),
            promptConfig: promptConfig,
            jsonSerializerOptions: jsonSerializerOptions,
            loggerFactory: loggerFactory);
    }

    /// <summary>
    /// Creates a <see cref="KernelFunction"/> instance for a prompt specified via a prompt template and a prompt template configuration.
    /// </summary>
    /// <param name="promptTemplate">Prompt template for the function, defined using the <see cref="PromptTemplateConfig.SemanticKernelTemplateFormat"/> template format.</param>
    /// <param name="promptConfig">Prompt template configuration.</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <returns>A function ready to use</returns>
    [RequiresUnreferencedCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    [RequiresDynamicCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    public static KernelFunction Create(
        IPromptTemplate promptTemplate,
        PromptTemplateConfig promptConfig,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(promptTemplate);
        Verify.NotNull(promptConfig);

        return new KernelFunctionFromPrompt(
            template: promptTemplate,
            promptConfig: promptConfig,
            logger: loggerFactory?.CreateLogger(typeof(KernelFunctionFactory)) ?? NullLogger.Instance);
    }

    /// <summary>
    /// Creates a <see cref="KernelFunction"/> instance for a prompt specified via a prompt template and a prompt template configuration.
    /// </summary>
    /// <param name="promptTemplate">Prompt template for the function, defined using the <see cref="PromptTemplateConfig.SemanticKernelTemplateFormat"/> template format.</param>
    /// <param name="promptConfig">Prompt template configuration.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization of various aspects of the function.</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <returns>A function ready to use</returns>
    public static KernelFunction Create(
        IPromptTemplate promptTemplate,
        PromptTemplateConfig promptConfig,
        JsonSerializerOptions jsonSerializerOptions,
        ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(promptTemplate);
        Verify.NotNull(promptConfig);
        Verify.NotNull(jsonSerializerOptions);

        return new KernelFunctionFromPrompt(
            template: promptTemplate,
            promptConfig: promptConfig,
            jsonSerializerOptions: jsonSerializerOptions,
            logger: loggerFactory?.CreateLogger(typeof(KernelFunctionFactory)) ?? NullLogger.Instance);
    }

    /// <inheritdoc/>
    protected override async ValueTask<FunctionResult> InvokeCoreAsync(
        Kernel kernel,
        KernelArguments arguments,
        CancellationToken cancellationToken = default)
    {
        AddDefaultValues(arguments);

        var promptRenderingResult = await RenderPromptAsync(
            kernel,
            arguments,
            isStreaming: false,
            cancellationToken).
            ConfigureAwait(false);

        // Return function result if it was set in prompt filter.
        if (promptRenderingResult.FunctionResult is not null)
        {
            promptRenderingResult.FunctionResult.RenderedPrompt = promptRenderingResult.RenderedPrompt;

            return promptRenderingResult.FunctionResult;
        }

        return promptRenderingResult.AIService switch
        {
            IChatCompletionService chatCompletion => await GetChatCompletionResultAsync(chatCompletion, kernel, promptRenderingResult, cancellationToken).
                ConfigureAwait(false),
            ITextGenerationService textGeneration => await GetTextGenerationResultAsync(textGeneration, kernel, promptRenderingResult, cancellationToken).
                ConfigureAwait(false),
            IChatClient chatClient => await this.GetChatClientResultAsync(chatClient, kernel, promptRenderingResult, cancellationToken).ConfigureAwait(false),
            // The service selector didn't find an appropriate service. This should only happen with a poorly implemented selector.
            _ => throw new NotSupportedException($"The AI service {promptRenderingResult.AIService.GetType()} is not supported. Supported services are {typeof(IChatCompletionService)} and {typeof(ITextGenerationService)}")
        };
    }

    /// <inheritdoc/>
    protected override async IAsyncEnumerable<TResult> InvokeStreamingCoreAsync<TResult>(
        Kernel kernel,
        KernelArguments arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AddDefaultValues(arguments);

        var result = await RenderPromptAsync(
            kernel,
            arguments,
            isStreaming: true,
            cancellationToken).
            ConfigureAwait(false);

        IAsyncEnumerable<object>? asyncReference = null;

        if (result.AIService is IChatCompletionService chatCompletion)
        {
            asyncReference = chatCompletion.GetStreamingChatMessageContentsAsync(result.RenderedPrompt, result.ExecutionSettings, kernel, cancellationToken);
        }
        else if (result.AIService is ITextGenerationService textGeneration)
        {
            asyncReference = textGeneration.GetStreamingTextContentsWithDefaultParserAsync(result.RenderedPrompt, result.ExecutionSettings, kernel, cancellationToken);
        }
        else if (result.AIService is IChatClient chatClient)
        {
            asyncReference = chatClient.GetStreamingResponseAsync(result.RenderedPrompt, result.ExecutionSettings, kernel, cancellationToken);
        }
        else
        {
            // The service selector didn't find an appropriate service. This should only happen with a poorly implemented selector.
            throw new NotSupportedException($"The AI service {result.AIService.GetType()} is not supported. Supported services are {typeof(IChatCompletionService)}, {typeof(ITextGenerationService)}, and {typeof(IChatClient)}");
        }

        await foreach (object content in asyncReference.ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (content is StreamingKernelContent kernelContent)
            {
                if (typeof(TResult) == typeof(string))
                {
                    yield return (TResult)(object)kernelContent.ToString();
                    continue;
                }

                if (content is TResult contentAsT)
                {
                    yield return contentAsT;
                    continue;
                }

                if (kernelContent.InnerContent is TResult innerContentAsT)
                {
                    yield return innerContentAsT;
                    continue;
                }

                if (typeof(TResult) == typeof(byte[]))
                {
                    if (content is StreamingKernelContent byteKernelContent)
                    {
                        yield return (TResult)(object)byteKernelContent.ToByteArray();
                        continue;
                    }
                }

                // Attempting to use the new Microsoft Extensions AI types will trigger automatic conversion of SK chat contents.
                if (typeof(ChatResponseUpdate).IsAssignableFrom(typeof(TResult))
                    && content is StreamingChatMessageContent streamingChatMessageContent)
                {
                    yield return (TResult)(object)streamingChatMessageContent.ToChatResponseUpdate();
                    continue;
                }
            }
            else if (content is ChatResponseUpdate chatUpdate)
            {
                if (typeof(TResult) == typeof(string))
                {
                    yield return (TResult)(object)chatUpdate.ToString();
                    continue;
                }

                if (chatUpdate is TResult contentAsT)
                {
                    yield return contentAsT;
                    continue;
                }

                if (chatUpdate.Contents is TResult contentListsAsT)
                {
                    yield return contentListsAsT;
                    continue;
                }

                if (chatUpdate.RawRepresentation is TResult rawRepresentationAsT)
                {
                    yield return rawRepresentationAsT;
                    continue;
                }

                if (typeof(Microsoft.Extensions.AI.AIContent).IsAssignableFrom(typeof(TResult)))
                {
                    // Return the first matching content type of an update if any
                    var updateContent = chatUpdate.Contents.FirstOrDefault(c => c is TResult);
                    if (updateContent is not null)
                    {
                        yield return (TResult)(object)updateContent;
                        continue;
                    }
                }

                if (typeof(TResult) == typeof(byte[]))
                {
                    DataContent? dataContent = (DataContent?)chatUpdate.Contents.FirstOrDefault(c => c is DataContent dataContent);
                    if (dataContent is not null)
                    {
                        yield return (TResult)(object)dataContent.Data.ToArray();
                        continue;
                    }
                }

                // Avoid breaking changes this transformation will be dropped once we migrate fully to Microsoft Extensions AI abstractions.
                // This is also necessary to don't break existing code using KernelContents when using IChatClient connectors.
                if (typeof(StreamingKernelContent).IsAssignableFrom(typeof(TResult)))
                {
                    yield return (TResult)(object)chatUpdate.ToStreamingChatMessageContent();
                    continue;
                }
            }

            throw new NotSupportedException($"The specific type {typeof(TResult)} is not supported. Support types are derivations of {typeof(StreamingKernelContent)}, {typeof(StreamingKernelContent)}, string, byte[], or a matching type for {typeof(StreamingKernelContent)}.{nameof(StreamingKernelContent.InnerContent)} property");
        }

        // There is no post cancellation check to override the result as the stream data was already sent.
    }

    /// <inheritdoc/>
    public override KernelFunction Clone(string? pluginName = null)
    {
        if (pluginName is not null)
        {
            Verify.NotNullOrWhiteSpace(pluginName, nameof(pluginName));
        }

        return new KernelFunctionFromPrompt(
            _promptTemplate,
            Name,
            pluginName,
            Description,
            Metadata.Parameters,
            JsonSerializerOptions,
            Metadata.ReturnParameter,
            ExecutionSettings as Dictionary<string, PromptExecutionSettings> ?? ExecutionSettings!.ToDictionary(kv => kv.Key, kv => kv.Value),
            _inputVariables,
            _logger);
    }

    [RequiresUnreferencedCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    [RequiresDynamicCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    private KernelFunctionFromPrompt(
        IPromptTemplate template,
        PromptTemplateConfig promptConfig,
        ILogger logger) : this(
        template,
        promptConfig.Name ?? CreateRandomFunctionName(),
        null,
        promptConfig.Description ?? string.Empty,
        promptConfig.GetKernelParametersMetadata(),
        promptConfig.GetKernelReturnParameterMetadata(),
        promptConfig.ExecutionSettings,
        promptConfig.InputVariables,
        logger)
    {
    }

    private KernelFunctionFromPrompt(
        IPromptTemplate template,
        PromptTemplateConfig promptConfig,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger logger) : this(
            template,
            promptConfig.Name ?? CreateRandomFunctionName(),
            null,
            promptConfig.Description ?? string.Empty,
            promptConfig.GetKernelParametersMetadata(jsonSerializerOptions),
            jsonSerializerOptions,
            promptConfig.GetKernelReturnParameterMetadata(jsonSerializerOptions),
            promptConfig.ExecutionSettings,
            promptConfig.InputVariables,
            logger)
    {
    }

    [RequiresUnreferencedCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    [RequiresDynamicCode("Uses reflection to handle various aspects of the function creation and invocation, making it incompatible with AOT scenarios.")]
    private KernelFunctionFromPrompt(
        IPromptTemplate template,
        string functionName,
        string? pluginName,
        string description,
        IReadOnlyList<KernelParameterMetadata> parameters,
        KernelReturnParameterMetadata? returnParameter,
        Dictionary<string, PromptExecutionSettings> executionSettings,
        List<InputVariable> inputVariables,
        ILogger logger) : base(
        functionName ?? CreateRandomFunctionName(),
        pluginName,
        description ?? string.Empty,
        parameters,
        returnParameter,
        executionSettings)
    {
        _logger = logger;

        _promptTemplate = template;

        _inputVariables = inputVariables.Select(iv => new InputVariable(iv)).
            ToList();
    }

    private KernelFunctionFromPrompt(
        IPromptTemplate template,
        string functionName,
        string? pluginName,
        string description,
        IReadOnlyList<KernelParameterMetadata> parameters,
        JsonSerializerOptions jsonSerializerOptions,
        KernelReturnParameterMetadata? returnParameter,
        Dictionary<string, PromptExecutionSettings> executionSettings,
        List<InputVariable> inputVariables,
        ILogger logger) : base(
            functionName ?? CreateRandomFunctionName(),
            pluginName,
            description ?? string.Empty,
            parameters,
            jsonSerializerOptions,
            returnParameter,
            executionSettings)
    {
        _logger = logger;

        _promptTemplate = template;
        _inputVariables = inputVariables.Select(iv => new InputVariable(iv)).ToList();
    }

    #region private

    private readonly ILogger _logger;

    private readonly List<InputVariable> _inputVariables;

    private readonly IPromptTemplate _promptTemplate;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => string.IsNullOrWhiteSpace(Description)
        ? Name
        : $"{Name} ({Description})";

    /// <summary>The measurement tag name for the model used.</summary>
    private const string MeasurementModelTagName = "semantic_kernel.function.model_id";

    /// <summary><see cref="Counter{T}"/> to record function invocation prompt token usage.</summary>
    private static readonly Histogram<long> s_invocationTokenUsagePrompt = s_meter.CreateHistogram<long>(
        name: "semantic_kernel.function.invocation.token_usage.prompt",
        unit: "{token}",
        description: "Measures the prompt token usage");

    /// <summary><see cref="Counter{T}"/> to record function invocation completion token usage.</summary>
    private static readonly Histogram<long> s_invocationTokenUsageCompletion = s_meter.CreateHistogram<long>(
        name: "semantic_kernel.function.invocation.token_usage.completion",
        unit: "{token}",
        description: "Measures the completion token usage");

    /// <summary>Add default values to the arguments if an argument is not defined</summary>
    private void AddDefaultValues(KernelArguments arguments)
    {
        foreach (var parameter in _inputVariables)
        {
            if (!arguments.ContainsName(parameter.Name) && parameter.Default is not null)
            {
                arguments[parameter.Name] = parameter.Default;
            }
        }
    }

    private async Task<PromptRenderingResult> RenderPromptAsync(
        Kernel kernel,
        KernelArguments arguments,
        bool isStreaming,
        CancellationToken cancellationToken)
    {
        var serviceSelector = kernel.ServiceSelector;

        IAIService? aiService = null;
        string renderedPrompt = string.Empty;

        // Try to use IChatCompletionService.
        if (serviceSelector.TrySelectAIService<IChatCompletionService>(
                kernel, this, arguments,
                out IChatCompletionService? chatService, out PromptExecutionSettings? executionSettings))
        {
            aiService = chatService;
        }
        else if (serviceSelector.TrySelectAIService<ITextGenerationService>(
            kernel, this, arguments,
            out ITextGenerationService? textService, out executionSettings))
        {
            aiService = textService;
        }
#pragma warning disable CA2000 // Dispose objects before losing scope
        else if (serviceSelector is IChatClientSelector chatClientServiceSelector
            && chatClientServiceSelector.TrySelectChatClient<IChatClient>(kernel, this, arguments, out var chatClient, out executionSettings))
        {
            // Resolves a ChatClient as AIService so it don't need to implement IChatCompletionService.
            aiService = new ChatClientAIService(chatClient);
        }

        if (aiService is null)
        {
            var message = new StringBuilder().Append("No service was found for any of the supported types: ").Append(typeof(IChatCompletionService)).Append(", ").Append(typeof(ITextGenerationService)).Append(", ").Append(typeof(IChatClient)).Append('.');
            if (this.ExecutionSettings is not null)
            {
                string serviceIds = string.Join("|", this.ExecutionSettings.Keys);
                if (!string.IsNullOrEmpty(serviceIds))
                {
                    message.Append(" Expected serviceIds: ").Append(serviceIds).Append('.');
                }

                string modelIds = string.Join("|", this.ExecutionSettings.Values.Select(model => model.ModelId));
                if (!string.IsNullOrEmpty(modelIds))
                {
                    message.Append(" Expected modelIds: ").Append(modelIds).Append('.');
                }
            }

            throw new KernelException(message.ToString());
        }
#pragma warning restore CA2000 // Dispose objects before losing scope

        Verify.NotNull(aiService);

        var renderingContext = await kernel.OnPromptRenderAsync(this, arguments, isStreaming, executionSettings, async (context) =>
            {
                renderedPrompt = await _promptTemplate.RenderAsync(kernel, context.Arguments, cancellationToken).
                    ConfigureAwait(false);

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Rendered prompt: {Prompt}", renderedPrompt);
                }

                context.RenderedPrompt = renderedPrompt;
            }, cancellationToken).
            ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(renderingContext.RenderedPrompt) &&
            !string.Equals(renderingContext.RenderedPrompt, renderedPrompt, StringComparison.OrdinalIgnoreCase))
        {
            renderedPrompt = renderingContext.RenderedPrompt!;

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Rendered prompt changed by prompt filter: {Prompt}", renderingContext.RenderedPrompt);
            }
        }

        return new(aiService, renderedPrompt)
        {
            ExecutionSettings = executionSettings,
            FunctionResult = renderingContext.Result
        };
    }

    /// <summary>Create a random, valid function name.</summary>
    internal static string CreateRandomFunctionName(string? prefix = "Function") => $"{prefix}_{Guid.NewGuid():N}";

    /// <summary>
    /// Captures usage details, including token information.
    /// </summary>
    [ExcludeFromCodeCoverage]
    private void CaptureUsageDetails(string? modelId, IReadOnlyDictionary<string, object?>? metadata, ILogger logger)
    {
        if (!logger.IsEnabled(LogLevel.Information) &&
            !s_invocationTokenUsageCompletion.Enabled &&
            !s_invocationTokenUsagePrompt.Enabled)
        {
            // Bail early to avoid unnecessary work.
            return;
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            logger.LogInformation("No model ID provided to capture usage details.");

            return;
        }

        if (metadata is null)
        {
            logger.LogInformation("No metadata provided to capture usage details.");

            return;
        }

        if (!metadata.TryGetValue("Usage", out object? usageObject) || usageObject is null)
        {
            logger.LogInformation("No usage details provided to capture usage details.");

            return;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "The warning is shown and should be addressed at the function creation site; there is no need to show it again at the function invocation sites.")]
        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "The warning is shown and should be addressed at the function creation site; there is no need to show it again at the function invocation sites.")]
        JsonElement SerializeToElement(object? value)
        {
            return JsonSerializer.SerializeToElement(value, JsonSerializerOptions);
        }

        var jsonObject = default(JsonElement);

        try
        {
            jsonObject = SerializeToElement(usageObject);
        }
        catch (Exception ex) when (ex is NotSupportedException)
        {
            logger.LogWarning(ex, "Error while parsing usage details from model result.");

            return;
        }

        if (jsonObject.TryGetProperty("PromptTokens", out var promptTokensJson) &&
            promptTokensJson.TryGetInt32(out int promptTokens) &&
            jsonObject.TryGetProperty("CompletionTokens", out var completionTokensJson) &&
            completionTokensJson.TryGetInt32(out int completionTokens))
        {
            TagList tags = new()
            {
                { MeasurementFunctionTagName, Name },
                { MeasurementModelTagName, modelId }
            };

            s_invocationTokenUsagePrompt.Record(promptTokens, in tags);
            s_invocationTokenUsageCompletion.Record(completionTokens, in tags);
        }
        else if (jsonObject.TryGetProperty("InputTokenCount", out var pascalInputTokensJson) &&
            pascalInputTokensJson.TryGetInt32(out int pascalInputTokens) &&
            jsonObject.TryGetProperty("OutputTokenCount", out var pascalOutputTokensJson) &&
            pascalOutputTokensJson.TryGetInt32(out int pascalOutputTokens))
        {
            TagList tags = new() {
                { MeasurementFunctionTagName, this.Name },
                { MeasurementModelTagName, modelId }
            };

            s_invocationTokenUsagePrompt.Record(pascalInputTokens, in tags);
            s_invocationTokenUsageCompletion.Record(pascalOutputTokens, in tags);
        }
        else if (jsonObject.TryGetProperty("inputTokenCount", out var inputTokensJson) &&
                 inputTokensJson.TryGetInt32(out int inputTokens) &&
                 jsonObject.TryGetProperty("outputTokenCount", out var outputTokensJson) &&
                 outputTokensJson.TryGetInt32(out int outputTokens))
        {
            TagList tags = new()
            {
                { MeasurementFunctionTagName, Name },
                { MeasurementModelTagName, modelId }
            };

            s_invocationTokenUsagePrompt.Record(inputTokens, in tags);
            s_invocationTokenUsageCompletion.Record(outputTokens, in tags);
        }
        else
        {
            logger.LogWarning("Unable to get token details from model result.");
        }
    }

    /// <summary>
    /// Captures usage details, including token information.
    /// </summary>
    private void CaptureUsageDetails(string? modelId, UsageDetails? usageDetails, ILogger logger)
    {
        if (!logger.IsEnabled(LogLevel.Information) &&
            !s_invocationTokenUsageCompletion.Enabled &&
            !s_invocationTokenUsagePrompt.Enabled)
        {
            // Bail early to avoid unnecessary work.
            return;
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            logger.LogInformation("No model ID provided to capture usage details.");
            return;
        }

        if (usageDetails is null)
        {
            logger.LogInformation("No usage details was provided.");
            return;
        }

        if (usageDetails.InputTokenCount.HasValue && usageDetails.OutputTokenCount.HasValue)
        {
            TagList tags = new() {
                { MeasurementFunctionTagName, this.Name },
                { MeasurementModelTagName, modelId }
            };
            s_invocationTokenUsagePrompt.Record(usageDetails.InputTokenCount.Value, in tags);
            s_invocationTokenUsageCompletion.Record(usageDetails.OutputTokenCount.Value, in tags);
        }
        else
        {
            logger.LogWarning("Unable to get token details from model result.");
        }
    }

    private async Task<FunctionResult> GetChatCompletionResultAsync(
        IChatCompletionService chatCompletion,
        Kernel kernel,
        PromptRenderingResult promptRenderingResult,
        CancellationToken cancellationToken)
    {
        var chatContents = await chatCompletion.GetChatMessageContentsAsync(
                promptRenderingResult.RenderedPrompt,
                promptRenderingResult.ExecutionSettings,
                kernel,
                cancellationToken).
            ConfigureAwait(false);

        if (chatContents is { Count: 0 })
        {
            return new FunctionResult(this, culture: kernel.Culture) { RenderedPrompt = promptRenderingResult.RenderedPrompt };
        }

        // Usage details are global and duplicated for each chat message content, use first one to get usage information
        var chatContent = chatContents[0];
        CaptureUsageDetails(chatContent.ModelId, chatContent.Metadata, _logger);

        // If collection has one element, return single result
        if (chatContents.Count == 1)
        {
            return new FunctionResult(this, chatContent, kernel.Culture, chatContent.Metadata) { RenderedPrompt = promptRenderingResult.RenderedPrompt };
        }

        // Otherwise, return multiple results
        return new FunctionResult(this, chatContents, kernel.Culture) { RenderedPrompt = promptRenderingResult.RenderedPrompt };
    }

    private async Task<FunctionResult> GetChatClientResultAsync(
       IChatClient chatClient,
       Kernel kernel,
       PromptRenderingResult promptRenderingResult,
       CancellationToken cancellationToken)
    {
        var chatResponse = await chatClient.GetResponseAsync(
            promptRenderingResult.RenderedPrompt,
            promptRenderingResult.ExecutionSettings,
            kernel,
            cancellationToken).ConfigureAwait(false);

        if (chatResponse.Messages is { Count: 0 })
        {
            return new FunctionResult(this, chatResponse)
            {
                Culture = kernel.Culture,
                RenderedPrompt = promptRenderingResult.RenderedPrompt
            };
        }

        var modelId = chatClient.GetService<ChatClientMetadata>()?.DefaultModelId;

        // Usage details are global and duplicated for each chat message content, use first one to get usage information
        this.CaptureUsageDetails(chatClient.GetService<ChatClientMetadata>()?.DefaultModelId, chatResponse.Usage, this._logger);

        return new FunctionResult(this, chatResponse)
        {
            Culture = kernel.Culture,
            RenderedPrompt = promptRenderingResult.RenderedPrompt,
            Metadata = chatResponse.AdditionalProperties,
        };
    }

    private async Task<FunctionResult> GetTextGenerationResultAsync(
        ITextGenerationService textGeneration,
        Kernel kernel,
        PromptRenderingResult promptRenderingResult,
        CancellationToken cancellationToken)
    {
        var textContents = await textGeneration.GetTextContentsWithDefaultParserAsync(
                promptRenderingResult.RenderedPrompt,
                promptRenderingResult.ExecutionSettings,
                kernel,
                cancellationToken).
            ConfigureAwait(false);

        if (textContents is { Count: 0 })
        {
            return new FunctionResult(this, culture: kernel.Culture) { RenderedPrompt = promptRenderingResult.RenderedPrompt };
        }

        // Usage details are global and duplicated for each text content, use first one to get usage information
        var textContent = textContents[0];
        CaptureUsageDetails(textContent.ModelId, textContent.Metadata, _logger);

        // If collection has one element, return single result
        if (textContents.Count == 1)
        {
            return new FunctionResult(this, textContent, kernel.Culture, textContent.Metadata) { RenderedPrompt = promptRenderingResult.RenderedPrompt };
        }

        // Otherwise, return multiple results
        return new FunctionResult(this, textContents, kernel.Culture) { RenderedPrompt = promptRenderingResult.RenderedPrompt };
    }

    #endregion

}
