// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.AI.ChatCompletion;

using System.Threading;
using System.Threading.Tasks;


/// <summary>
/// Interface for chat completion results
/// </summary>
public interface IChatResult : IResultBase
{
    /// <summary>
    /// Get the chat message from the result.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>Current chat message content</returns>
    Task<ChatMessage> GetChatMessageAsync(CancellationToken cancellationToken = default);
}
