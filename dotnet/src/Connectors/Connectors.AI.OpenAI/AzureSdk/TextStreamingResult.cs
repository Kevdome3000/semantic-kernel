// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Orchestration;
using SemanticKernel.AI.TextCompletion;


internal sealed class TextStreamingResult : ITextStreamingResult, ITextResult
{
    private readonly StreamingChoice _choice;

    public ModelResult ModelResult { get; }


    public TextStreamingResult(StreamingCompletions resultData, StreamingChoice choice)
    {
        ModelResult = new ModelResult(resultData);
        _choice = choice;
    }


    public async Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
    {
        var fullMessage = new StringBuilder();

        await foreach (var message in _choice.GetTextStreaming(cancellationToken).ConfigureAwait(false))
        {
            fullMessage.Append(message);
        }

        return fullMessage.ToString();
    }


    public IAsyncEnumerable<string> GetCompletionStreamingAsync(CancellationToken cancellationToken = default) => _choice.GetTextStreaming(cancellationToken);
}
