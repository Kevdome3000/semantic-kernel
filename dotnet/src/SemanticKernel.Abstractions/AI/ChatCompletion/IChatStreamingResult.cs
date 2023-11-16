// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.AI.ChatCompletion;

using System.Collections.Generic;
using System.Threading;


/// <summary>
/// Interface for chat completion streaming results
/// </summary>
public interface IChatStreamingResult : IResultBase
{
    /// <summary>
    /// Get the chat message from the streaming result.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>Current chat message streaming content</returns>
    IAsyncEnumerable<ChatMessage> GetStreamingChatMessageAsync(CancellationToken cancellationToken = default);
}
