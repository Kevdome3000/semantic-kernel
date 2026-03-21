// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SemanticKernel.Agents.OpenAI;

/// <summary>
/// Represents a conversation thread for an OpenAI Response API based agent when store is enabled.
/// </summary>
public sealed class OpenAIResponseAgentThread : AgentThread
{
    private readonly ResponsesClient _client;
    private bool _isDeleted;


    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIResponseAgentThread"/> class.
    /// </summary>
    /// <param name="client">The agents client to use for interacting with responses.</param>
    public OpenAIResponseAgentThread(ResponsesClient client)
    {
        Verify.NotNull(client);

        _client = client;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIResponseAgentThread"/> class that resumes an existing response.
    /// </summary>
    /// <param name="client">The agents client to use for interacting with responses.</param>
    /// <param name="responseId">The ID of an existing response to resume.</param>
    public OpenAIResponseAgentThread(ResponsesClient client, string responseId)
    {
        Verify.NotNull(client);
        Verify.NotNull(responseId);

        _client = client;
        ResponseId = responseId;
    }


    /// <summary>
    /// The current response id.
    /// </summary>
    internal string? ResponseId { get; set; }

    /// <inheritdoc />
    public override string? Id => ResponseId;


    /// <inheritdoc />
    protected override Task<string?> CreateInternalAsync(CancellationToken cancellationToken = default)
    {
        if (_isDeleted)
        {
            throw new InvalidOperationException("This thread has been deleted and cannot be recreated.");
        }

        // Id will not be available until after a message is sent
        return Task.FromResult<string?>(null);
    }


    /// <inheritdoc />
    protected override async Task DeleteInternalAsync(CancellationToken cancellationToken = default)
    {
        if (_isDeleted)
        {
            return;
        }

        if (ResponseId is null)
        {
            throw new InvalidOperationException("This thread cannot be deleted, since it has not been created.");
        }

        try
        {
            await _client.DeleteResponseAsync(ResponseId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new AgentThreadOperationException("The thread could not be deleted due to an error response from the service.", ex);
        }

        _isDeleted = true;
    }


    /// <inheritdoc/>
    protected override Task OnNewMessageInternalAsync(ChatMessageContent newMessage, CancellationToken cancellationToken = default)
    {
        if (_isDeleted)
        {
            throw new InvalidOperationException("This thread has been deleted and cannot be used anymore.");
        }

        return Task.CompletedTask;
    }


    /// <inheritdoc />
    public async IAsyncEnumerable<ChatMessageContent> GetMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_isDeleted)
        {
            throw new InvalidOperationException("This thread has been deleted and cannot be used anymore.");
        }

        if (!string.IsNullOrEmpty(ResponseId))
        {
            var collectionResult = _client.GetResponseInputItemsAsync(ResponseId, cancellationToken).ConfigureAwait(false);

            await foreach (var responseItem in collectionResult)
            {
                var messageContent = responseItem.ToChatMessageContent();

                if (messageContent is not null)
                {
                    yield return messageContent;
                }
            }
        }
    }
}
