// Copyright (c) Microsoft. All rights reserved.

#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace Microsoft.SemanticKernel;

using Extensions.Logging;
using Extensions.Logging.Abstractions;


/// <summary>
/// Default implementation of <see cref="IPromptTemplateFactory"/> for the semantic-kernel prompt template format.
/// </summary>
public sealed class KernelPromptTemplateFactory : IPromptTemplateFactory
{
    private readonly ILoggerFactory _loggerFactory;


    /// <summary>
    /// Initializes a new instance of the <see cref="KernelPromptTemplateFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public KernelPromptTemplateFactory(ILoggerFactory? loggerFactory = null)
    {
        this._loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }


    /// <inheritdoc/>
    public IPromptTemplate Create(PromptTemplateConfig promptConfig)
    {
        if (promptConfig.TemplateFormat.Equals(PromptTemplateConfig.SemanticKernelTemplateFormat, System.StringComparison.Ordinal))
        {
            return new KernelPromptTemplate(promptConfig, this._loggerFactory);
        }

        throw new KernelException($"Prompt template format {promptConfig.TemplateFormat} is not supported.");
    }
}
