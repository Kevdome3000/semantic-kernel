// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Functions;

using System;
using AI;
using AI.TextCompletion;
using Orchestration;
using Services;


/// <summary>
/// Added for backward compatibility only, this will be removed when ISKFunction.SetAIService and ISKFunction.SetAIConfiguration are removed.
/// </summary>
[Obsolete("Remove this when ISKFunction.SetAIService and ISKFunction.SetAIConfiguration are removed.")]
internal class DelegatingAIServiceSelector : IAIServiceSelector
{
    internal Func<ITextCompletion>? ServiceFactory { get; set; }
    internal AIRequestSettings? RequestSettings { get; set; }


    /// <inheritdoc/>
    public (T?, AIRequestSettings?) SelectAIService<T>(SKContext context, ISKFunction skfunction) where T : IAIService
    {
        return ((T?)this.ServiceFactory?.Invoke() ?? context.ServiceProvider.GetService<T>(null), this.RequestSettings ?? skfunction.RequestSettings);
    }
}
