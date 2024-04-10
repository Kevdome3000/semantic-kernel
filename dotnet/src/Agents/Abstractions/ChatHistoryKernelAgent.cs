// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.SemanticKernel.Agents;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


/// <summary>
/// A <see cref="KernelAgent"/> specialization bound to a <see cref="ChatHistoryChannel"/>.
/// </summary>
public abstract class ChatHistoryKernelAgent : KernelAgent, IChatHistoryHandler
{

    /// <inheritdoc/>
    protected internal sealed override IEnumerable<string> GetChannelKeys()
    {
        yield return typeof(ChatHistoryChannel).FullName;
    }


    /// <inheritdoc/>
    protected internal sealed override Task<AgentChannel> CreateChannelAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<AgentChannel>(new ChatHistoryChannel());
    }


    /// <inheritdoc/>
    public abstract IAsyncEnumerable<ChatMessageContent> InvokeAsync(
        IReadOnlyList<ChatMessageContent> history,
        CancellationToken cancellationToken = default);

}
