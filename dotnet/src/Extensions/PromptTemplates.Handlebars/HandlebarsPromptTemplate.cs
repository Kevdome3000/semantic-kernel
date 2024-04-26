// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.PromptTemplates.Handlebars;

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Extensions.Logging;
using Extensions.Logging.Abstractions;
using HandlebarsDotNet;
using HandlebarsDotNet.Helpers;
using Helpers;


/// <summary>
/// Represents a Handlebars prompt template.
/// </summary>
internal sealed class HandlebarsPromptTemplate : IPromptTemplate
{

    /// <summary>
    /// Default options for built-in Handlebars helpers.
    /// </summary>
    /// TODO [@teresaqhoang]: Support override of default options
    private readonly HandlebarsPromptTemplateOptions _options;


    /// <summary>
    /// Constructor for Handlebars PromptTemplate.
    /// </summary>
    /// <param name="promptConfig">Prompt template configuration</param>
    /// <param name="allowUnsafeContent">Flag indicating whether to allow unsafe content</param>
    /// <param name="options">Handlebars prompt template options</param>
    internal HandlebarsPromptTemplate(PromptTemplateConfig promptConfig, bool allowUnsafeContent = false, HandlebarsPromptTemplateOptions? options = null)
    {
        _allowUnsafeContent = allowUnsafeContent;
        _loggerFactory ??= NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger(typeof(HandlebarsPromptTemplate));
        _promptModel = promptConfig;
        _options = options ?? new HandlebarsPromptTemplateOptions();
    }


    /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<string> RenderAsync(Kernel kernel, KernelArguments? arguments = null, CancellationToken cancellationToken = default)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        Verify.NotNull(kernel);

        arguments = GetVariables(kernel, arguments);
        var handlebarsInstance = Handlebars.Create();

        // Register kernel, system, and any custom helpers
        RegisterHelpers(handlebarsInstance, kernel, arguments, cancellationToken);

        var template = handlebarsInstance.Compile(_promptModel.Template);

        return WebUtility.HtmlDecode(template(arguments).
            Trim());
    }


    #region private

    private readonly ILoggerFactory _loggerFactory;

    private readonly ILogger _logger;

    private readonly PromptTemplateConfig _promptModel;

    private readonly bool _allowUnsafeContent;


    /// <summary>
    /// Registers kernel, system, and any custom helpers.
    /// </summary>
    private void RegisterHelpers(
        IHandlebars handlebarsInstance,
        Kernel kernel,
        KernelArguments arguments,
        CancellationToken cancellationToken = default)
    {
        // Add SK's built-in system helpers
        KernelSystemHelpers.Register(handlebarsInstance, kernel, arguments, _options);

        // Add built-in helpers from the HandlebarsDotNet library
        HandlebarsHelpers.Register(handlebarsInstance, options =>
        {
            options.PrefixSeparator = _options.PrefixSeparator;
            options.Categories = _options.Categories;
            options.UseCategoryPrefix = _options.UseCategoryPrefix;
            options.CustomHelperPaths = _options.CustomHelperPaths;
        });

        // Add helpers for kernel functions
        KernelFunctionHelpers.Register(handlebarsInstance, kernel, arguments, _promptModel,
            _allowUnsafeContent, _options.PrefixSeparator,
            cancellationToken);

        // Add any custom helpers
        _options.RegisterCustomHelpers?.Invoke(
            (name, customHelper)
                => KernelHelpersUtils.RegisterHelperSafe(handlebarsInstance, name, customHelper),
            _options,
            arguments);
    }


    /// <summary>
    /// Gets the variables for the prompt template, including setting any default values from the prompt config.
    /// </summary>
    private KernelArguments GetVariables(Kernel kernel, KernelArguments? arguments)
    {
        KernelArguments result = [];

        foreach (var p in _promptModel.InputVariables)
        {
            if (p.Default == null || p.Default is string stringDefault && stringDefault.Length == 0)
            {
                continue;
            }

            result[p.Name] = p.Default;
        }

        if (arguments is not null)
        {
            foreach (var kvp in arguments)
            {
                if (kvp.Value is not null)
                {
                    var value = kvp.Value;

                    if (ShouldEncodeTags(_promptModel, kvp.Key, kvp.Value))
                    {
                        value = HttpUtility.HtmlEncode(value.ToString());
                    }

                    result[kvp.Key] = value;
                }
            }
        }

        return result;
    }


    private bool ShouldEncodeTags(PromptTemplateConfig promptTemplateConfig, string propertyName, object? propertyValue)
    {
        if (propertyValue is null || propertyValue is not string || _allowUnsafeContent)
        {
            return false;
        }

        foreach (var inputVariable in promptTemplateConfig.InputVariables)
        {
            if (inputVariable.Name == propertyName)
            {
                return !inputVariable.AllowUnsafeContent;
            }
        }

        return true;
    }

    #endregion


}
