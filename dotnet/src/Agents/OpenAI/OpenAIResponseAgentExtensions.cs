// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using MAAI = Microsoft.Agents.AI;

namespace Microsoft.SemanticKernel.Agents.OpenAI;

/// <summary>
/// Exposes a Semantic Kernel Agent Framework <see cref="OpenAIResponseAgent"/> as a Microsoft Agent Framework <see cref="MAAI.AIAgent"/>.
/// </summary>
public static class OpenAIResponseAgentExtensions
{
    /// <summary>
    /// Exposes a Semantic Kernel Agent Framework <see cref="OpenAIResponseAgent"/> as a Microsoft Agent Framework <see cref="MAAI.AIAgent"/>.
    /// </summary>
    /// <param name="responseAgent">The Semantic Kernel <see cref="OpenAIResponseAgent"/> to expose as a Microsoft Agent Framework <see cref="MAAI.AIAgent"/>.</param>
    /// <returns>The Semantic Kernel Agent Framework <see cref="Agent"/> exposed as a Microsoft Agent Framework <see cref="MAAI.AIAgent"/></returns>
    [Experimental("SKEXP0110")]
    public static MAAI.AIAgent AsAIAgent(this OpenAIResponseAgent responseAgent)
    {
        return responseAgent.AsAIAgent(
            () => responseAgent.StoreEnabled
                ? new OpenAIResponseAgentThread(responseAgent.Client)
                : new ChatHistoryAgentThread(),
            (json, options) =>
            {
                if (responseAgent.StoreEnabled)
                {
                    var agentId = json.Deserialize<string>();
                    return agentId is null
                        ? new OpenAIResponseAgentThread(responseAgent.Client)
                        : new OpenAIResponseAgentThread(responseAgent.Client, agentId);
                }

                var chatHistory = json.Deserialize<ChatHistory>();
                return chatHistory is null
                    ? new ChatHistoryAgentThread()
                    : new ChatHistoryAgentThread(chatHistory);
            },
            (thread, options) => responseAgent.StoreEnabled
                ? JsonSerializer.SerializeToElement((thread as OpenAIResponseAgentThread)?.Id)
                : JsonSerializer.SerializeToElement((thread as ChatHistoryAgentThread)?.ChatHistory));
    }
}
