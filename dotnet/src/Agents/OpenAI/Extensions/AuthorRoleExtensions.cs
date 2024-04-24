// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.SemanticKernel.Agents.OpenAI;

using ChatCompletion;
using global::Azure.AI.OpenAI.Assistants;


internal static class AuthorRoleExtensions
{

    /// <summary>
    /// Convert an <see cref="AuthorRole"/> to a <see cref="MessageRole"/>
    /// within <see cref="OpenAIAssistantChannel"/>.  A thread message may only be of
    /// two roles: User or Assistant.
    /// </summary>
    /// <remarks>
    /// The agent framework disallows any system message for all agents as part
    /// of the agent conversation.  Should this conversation method experience a
    /// system message, it will be converted to assistant role.
    /// </remarks>
    public static MessageRole ToMessageRole(this AuthorRole authorRole) =>
        authorRole == AuthorRole.User
            ? MessageRole.User
            : MessageRole.Assistant;

}
