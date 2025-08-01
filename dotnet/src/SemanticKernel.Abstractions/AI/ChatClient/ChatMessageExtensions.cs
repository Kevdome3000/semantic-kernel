﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.AI;

namespace Microsoft.SemanticKernel.ChatCompletion;

internal static class ChatMessageExtensions
{
    /// <summary>Converts a <see cref="ChatMessage"/> to a <see cref="ChatMessageContent"/>.</summary>
    internal static ChatMessageContent ToChatMessageContent(this ChatMessage message, ChatResponse? response = null)
    {
        ChatMessageContent result = new()
        {
            ModelId = response?.ModelId,
            AuthorName = message.AuthorName,
            InnerContent = response?.RawRepresentation ?? message.RawRepresentation,
            Metadata = new AdditionalPropertiesDictionary(message.AdditionalProperties ?? []) { ["Usage"] = response?.Usage },
            Role = new AuthorRole(message.Role.Value),
        };

        foreach (AIContent content in message.Contents)
        {
            KernelContent? resultContent = content switch
            {
                Microsoft.Extensions.AI.TextContent tc => new TextContent(tc.Text),
                DataContent dc when dc.HasTopLevelMediaType("image") => new ImageContent(dc.Uri),
                UriContent uc when uc.HasTopLevelMediaType("image") => new ImageContent(uc.Uri),
                DataContent dc when dc.HasTopLevelMediaType("audio") => new AudioContent(dc.Uri),
                UriContent uc when uc.HasTopLevelMediaType("audio") => new AudioContent(uc.Uri),
                DataContent dc => new BinaryContent(dc.Uri),
                UriContent uc => new BinaryContent(uc.Uri),
                Microsoft.Extensions.AI.FunctionCallContent fcc => new FunctionCallContent(
                    functionName: fcc.Name,
                    id: fcc.CallId,
                    arguments: fcc.Arguments is not null ? new(fcc.Arguments) : null),
                Microsoft.Extensions.AI.FunctionResultContent frc => new FunctionResultContent(
                    functionName: GetFunctionCallContent(frc.CallId)?.Name,
                    callId: frc.CallId,
                    result: frc.Result),
                _ => null
            };

            if (resultContent is not null)
            {
                resultContent.Metadata = content.AdditionalProperties;
                resultContent.InnerContent = content.RawRepresentation;
                resultContent.ModelId = response?.ModelId;
                result.Items.Add(resultContent);
            }
        }

        return result;

        Microsoft.Extensions.AI.FunctionCallContent? GetFunctionCallContent(string callId)
            => response?.Messages
                .Select(m => m.Contents
                .FirstOrDefault(c => c is Microsoft.Extensions.AI.FunctionCallContent fcc && fcc.CallId == callId) as Microsoft.Extensions.AI.FunctionCallContent)
                    .FirstOrDefault(fcc => fcc is not null);
    }

    /// <summary>Converts a list of <see cref="ChatMessage"/> to a <see cref="ChatHistory"/>.</summary>
    internal static ChatHistory ToChatHistory(this IEnumerable<ChatMessage> chatMessages)
    {
        ChatHistory chatHistory = [];
        foreach (var message in chatMessages)
        {
            chatHistory.Add(message.ToChatMessageContent());
        }
        return chatHistory;
    }
}
