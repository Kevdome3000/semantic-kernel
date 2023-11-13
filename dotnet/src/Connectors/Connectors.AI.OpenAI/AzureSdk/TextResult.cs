// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;

using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Orchestration;
using SemanticKernel.AI.TextCompletion;


internal sealed class TextResult : ITextResult
{
    private readonly ModelResult _modelResult;
    private readonly Choice _choice;


    public TextResult(Completions resultData, Choice choice)
    {
        _modelResult = new ModelResult(new TextModelResult(resultData, choice));
        _choice = choice;
    }


    public ModelResult ModelResult => _modelResult;

    public Task<string> GetCompletionAsync(CancellationToken cancellationToken = default) => Task.FromResult(_choice.Text);
}
