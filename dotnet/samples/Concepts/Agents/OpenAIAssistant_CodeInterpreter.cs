﻿// Copyright (c) Microsoft. All rights reserved.
namespace Agents;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;


/// <summary>
/// Demonstrate using code-interpreter on <see cref="OpenAIAssistantAgent"/> .
/// </summary>
public class OpenAIAssistant_CodeInterpreter(ITestOutputHelper output) : BaseTest(output)
{

    protected override bool ForceOpenAI => true;


    [Fact]
    public async Task UseCodeInterpreterToolWithOpenAIAssistantAgentAsync()
    {
        // Define the agent
        OpenAIAssistantAgent agent =
            await OpenAIAssistantAgent.CreateAsync(
                kernel: new(),
                config: new(this.ApiKey, this.Endpoint),
                new()
                {
                    EnableCodeInterpreter = true, // Enable code-interpreter
                    ModelId = this.Model,
                });

        // Create a chat for agent interaction.
        AgentGroupChat chat = new();

        // Respond to user input
        try
        {
            await InvokeAgentAsync("Use code to determine the values in the Fibonacci sequence that that are less then the value of 101?");
        }
        finally
        {
            await agent.DeleteAsync();
        }

        // Local function to invoke agent and display the conversation messages.
        async Task InvokeAgentAsync(string input)
        {
            chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));

            Console.WriteLine($"# {AuthorRole.User}: '{input}'");

            await foreach (var content in chat.InvokeAsync(agent))
            {
                Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
            }
        }
    }

}
