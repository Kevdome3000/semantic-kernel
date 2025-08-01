// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Services;

namespace Microsoft.SemanticKernel.ChatCompletion;

/// <summary>
/// Allow <see cref="IChatClient"/> to be used as an <see cref="IAIService"/> in a <see cref="IAIServiceSelector"/>
/// </summary>
internal sealed class ChatClientAIService : IAIService, IChatClient
{
    private readonly IChatClient _chatClient;

    /// <summary>
    /// Storage for AI service attributes.
    /// </summary>
    internal Dictionary<string, object?> _internalAttributes { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatClientAIService"/> class.
    /// </summary>
    /// <param name="chatClient">Target <see cref="IChatClient"/>.</param>
    internal ChatClientAIService(IChatClient chatClient)
    {
        Verify.NotNull(chatClient);
        _chatClient = chatClient;

        var metadata = _chatClient.GetService<ChatClientMetadata>();
        Verify.NotNull(metadata);

        _internalAttributes[AIServiceExtensions.ModelIdKey] = metadata.DefaultModelId;
        _internalAttributes[nameof(metadata.ProviderName)] = metadata.ProviderName;
        _internalAttributes[nameof(metadata.ProviderUri)] = metadata.ProviderUri;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Attributes => _internalAttributes;

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => _chatClient.GetResponseAsync(messages, options, cancellationToken);

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => _chatClient.GetService(serviceType, serviceKey);

    /// <inheritdoc />
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => _chatClient.GetStreamingResponseAsync(messages, options, cancellationToken);
}
