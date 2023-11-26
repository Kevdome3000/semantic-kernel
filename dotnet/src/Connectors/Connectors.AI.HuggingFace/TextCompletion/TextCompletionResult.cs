// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.HuggingFace.TextCompletion;

using System.Threading;
using System.Threading.Tasks;
using Orchestration;
using SemanticKernel.AI.TextCompletion;


internal sealed class TextCompletionResult : ITextResult
{
    public TextCompletionResult(TextCompletionResponse responseData) =>
        this.ModelResult = new ModelResult(responseData);


    public ModelResult ModelResult { get; }


    public Task<string> GetCompletionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(this.ModelResult.GetResult<TextCompletionResponse>().Text ?? string.Empty);
}
