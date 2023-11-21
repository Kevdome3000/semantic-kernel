// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Orchestration;
using SemanticKernel.AI.ChatCompletion;
using SemanticKernel.AI.TextCompletion;
using static FunctionCalling.Extensions.ChatMessageExtensions;
using ChatMessage = SemanticKernel.AI.ChatCompletion.ChatMessage;


internal sealed class ChatResult : IChatResult, ITextResult
{
    private readonly ChatChoice _choice;
    private readonly bool _isFunctionCall;


    public ChatResult(ChatCompletions resultData, ChatChoice choice, bool isFunctionCall = false)
    {
        Verify.NotNull(choice);
        _choice = choice;
        ModelResult = new ModelResult(new ChatModelResult(resultData, choice));
        _isFunctionCall = isFunctionCall;
    }


    public ModelResult ModelResult { get; }


    public Task<ChatMessage> GetChatMessageAsync(CancellationToken cancellationToken = default)
    {
        if (!_isFunctionCall)
        {
            return Task.FromResult((ChatMessage)new AzureOpenAIChatMessage(_choice.Message));
        }

        var content = _choice.Message.FunctionCall.Arguments;
        var functionName = _choice.Message.FunctionCall.Name;

        try
        {
            // if the contents first character is not a curly brace, then clean the json
            if (IsValidJson(content))
            {
                return Task.FromResult((ChatMessage)new AzureOpenAIChatMessage(AuthorRole.Assistant.Label, content, functionName));
            }
        }
        catch (JsonException e)
        {
            Console.WriteLine(e);
            content = CleanJson(content);

            Console.WriteLine($"Cleaned Json for Function {_choice.Message.FunctionCall.Name}: \n" +
                              $"Original Version: {_choice.Message.FunctionCall.Arguments} \n" +
                              $"Cleaned Version: {content}");
        }

        return Task.FromResult((ChatMessage)new AzureOpenAIChatMessage(AuthorRole.Assistant.Label, content, functionName));
    }


    public async Task<string> GetCompletionAsync(CancellationToken cancellationToken = default)
    {
        if (!_isFunctionCall)
        {
            return _choice.Message.Content;
        }

        var message = await GetChatMessageAsync(cancellationToken).ConfigureAwait(false);
        return message.Content;
    }
}
