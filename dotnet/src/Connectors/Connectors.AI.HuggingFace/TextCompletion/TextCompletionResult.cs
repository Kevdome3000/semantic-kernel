// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.HuggingFace.TextCompletion;

using System.Threading;
using System.Threading.Tasks;
using Orchestration;
using SemanticKernel.AI.TextCompletion;


internal sealed class TextCompletionResult : ITextResult
{
    private readonly ModelResult _responseData;


    public TextCompletionResult(TextCompletionResponse responseData)
    {
        this._responseData = new ModelResult(responseData);
    }


    public ModelResult ModelResult => this._responseData;


    public Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this._responseData.GetResult<TextCompletionResponse>().Text ?? string.Empty);
    }
}
