// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.AI.OpenAI.AzureSdk;

using System;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using SemanticKernel.AI.ChatCompletion;
using ChatMessage = SemanticKernel.AI.ChatCompletion.ChatMessage;


/// <summary>
/// Chat message representation fir Azure OpenAI
/// </summary>
public class AzureOpenAIChatMessage : ChatMessage
{
    /// <summary>
    /// Exposes the underlying OpenAI SDK chat message representation
    /// </summary>
    public Azure.AI.OpenAI.ChatMessage? InnerChatMessage { get; }

    /// <summary>
    ///  The name of the function call if the message is a function call.
    /// </summary>
    [JsonPropertyName("function")]
    public string? FunctionName { get; }

    /// <summary>
    /// Exposes the underlying OpenAI SDK function call chat message representation
    /// </summary>
    public FunctionCall FunctionCall
        => InnerChatMessage?.FunctionCall ?? throw new NotSupportedException("Function call is not supported");


    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAIChatMessage"/> class.
    /// </summary>
    /// <param name="message">OpenAI SDK chat message representation</param>
    public AzureOpenAIChatMessage(Azure.AI.OpenAI.ChatMessage message)
        : base(new AuthorRole(message.Role.ToString()), message.Content) => InnerChatMessage = message;


    /// <summary>
    /// Initializes a new instance of the <see cref="AzureOpenAIChatMessage"/> class.
    /// </summary>
    /// <param name="role">Role of the author of the message.</param>
    /// <param name="content">Content of the message.</param>
    /// <param name="functionName">the name of the function call if the message is a function call.</param>
    public AzureOpenAIChatMessage(string role, string content, string? functionName = null)
        : base(new AuthorRole(role), content) => FunctionName = functionName;
}
