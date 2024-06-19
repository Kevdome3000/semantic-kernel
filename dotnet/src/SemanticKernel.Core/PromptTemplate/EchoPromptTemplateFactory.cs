// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel;

using System.Diagnostics.CodeAnalysis;


/// <summary>
/// Provides an implementation of <see cref="IPromptTemplateFactory"/> which creates no operation instances of <see cref="IPromptTemplate"/>.
/// </summary>
public sealed class EchoPromptTemplateFactory : IPromptTemplateFactory
{

    /// <summary>
    /// Singleton instance of <see cref="EchoPromptTemplateFactory"/>.
    /// </summary>
    public static EchoPromptTemplateFactory Instance { get; } = new EchoPromptTemplateFactory();


    /// <inheritdoc/>
    public bool TryCreate(PromptTemplateConfig templateConfig, [NotNullWhen(true)] out IPromptTemplate? result)
    {
        result = new EchoPromptTemplate(templateConfig);

        return true;
    }

}
