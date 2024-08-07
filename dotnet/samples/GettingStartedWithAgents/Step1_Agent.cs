﻿// Copyright (c) Microsoft. All rights reserved.
namespace GettingStarted;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;


/// <summary>
/// Demonstrate creation of <see cref="ChatCompletionAgent"/> and
/// eliciting its response to three explicit user messages.
/// </summary>
public class Step1_Agent(ITestOutputHelper output) : BaseTest(output)
{

    private const string ParrotName = "Parrot";

    private const string ParrotInstructions = "Repeat the user message in the voice of a pirate and then end with a parrot sound.";


    [Fact]
    public async Task UseSingleChatCompletionAgentAsync()
    {
        // Define the agent
        ChatCompletionAgent agent =
            new()
            {
                Name = ParrotName,
                Instructions = ParrotInstructions,
                Kernel = this.CreateKernelWithChatCompletion(),
            };

        /// Create the chat history to capture the agent interaction.
        ChatHistory chat = [];

        // Respond to user input
        await InvokeAgentAsync("Fortune favors the bold.");
        await InvokeAgentAsync("I came, I saw, I conquered.");
        await InvokeAgentAsync("Practice makes perfect.");

        // Local function to invoke agent and display the conversation messages.
        async Task InvokeAgentAsync(string input)
        {
            chat.Add(new ChatMessageContent(AuthorRole.User, input));

            Console.WriteLine($"# {AuthorRole.User}: '{input}'");

            await foreach (ChatMessageContent content in agent.InvokeAsync(chat))
            {
                chat.Add(content);

                Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
            }
        }
    }

}
