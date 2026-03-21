// Copyright (c) Microsoft. All rights reserved.

using MAAI = Microsoft.Agents.AI;
using MEAI = Microsoft.Extensions.AI;

namespace Microsoft.SemanticKernel.Agents;

/// <summary>
/// Exposes a Semantic Kernel Agent Framework <see cref="Agent"/> as a Microsoft Agent Framework <see cref="MAAI.AIAgent"/>.
/// </summary>
[Experimental("SKEXP0110")]
internal sealed class SemanticKernelAIAgent : MAAI.AIAgent
{
    private readonly Agent _innerAgent;
    private readonly Func<AgentThread> _threadFactory;
    private readonly Func<JsonElement, JsonSerializerOptions?, AgentThread> _threadDeserializationFactory;
    private readonly Func<AgentThread, JsonSerializerOptions?, JsonElement> _threadSerializer;


    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticKernelAIAgent"/> class.
    /// </summary>
    /// <param name="semanticKernelAgent">The Semantic Kernel <see cref="Agent"/> to expose as a Microsoft Agent Framework <see cref="MAAI.AIAgent"/>.</param>
    /// <param name="threadFactory">A factory method to create the required <see cref="AgentThread"/> type to use with the agent.</param>
    /// <param name="threadDeserializationFactory">A factory method to deserialize the required <see cref="AgentThread"/> type.</param>
    /// <param name="threadSerializer">A method to serialize the <see cref="AgentThread"/> type.</param>
    public SemanticKernelAIAgent(
        Agent semanticKernelAgent,
        Func<AgentThread> threadFactory,
        Func<JsonElement, JsonSerializerOptions?, AgentThread> threadDeserializationFactory,
        Func<AgentThread, JsonSerializerOptions?, JsonElement> threadSerializer)
    {
        Throw.IfNull(semanticKernelAgent);
        Throw.IfNull(threadFactory);
        Throw.IfNull(threadDeserializationFactory);
        Throw.IfNull(threadSerializer);

        _innerAgent = semanticKernelAgent;
        _threadFactory = threadFactory;
        _threadDeserializationFactory = threadDeserializationFactory;
        _threadSerializer = threadSerializer;
    }


    /// <inheritdoc />
    public override string Id => _innerAgent.Id;

    /// <inheritdoc />
    public override string? Name => _innerAgent.Name;

    /// <inheritdoc />
    public override string? Description => _innerAgent.Description;


    /// <inheritdoc />
    public override MAAI.AgentThread DeserializeThread(JsonElement serializedThread, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return new SemanticKernelAIAgentThread(this._threadDeserializationFactory(serializedThread, jsonSerializerOptions), _threadSerializer);
    }


    /// <inheritdoc />
    public override MAAI.AgentThread GetNewThread()
    {
        return new SemanticKernelAIAgentThread(this._threadFactory(), _threadSerializer);
    }


    /// <inheritdoc />
    public override async Task<MAAI.AgentRunResponse> RunAsync(
        IEnumerable<ChatMessage> messages,
        MAAI.AgentThread? thread = null,
        MAAI.AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        thread ??= GetNewThread();

        if (thread is not SemanticKernelAIAgentThread typedThread)
        {
            throw new InvalidOperationException("The provided thread is not compatible with the agent. Only threads created by the agent can be used.");
        }

        List<ChatMessage> responseMessages = [];
        var invokeOptions = new AgentInvokeOptions
        {
            OnIntermediateMessage = msg =>
            {
                // As a backwards compatibility measure, ChatCompletionService inserts the function result
                // as a text message followed by a function result message. If we detect that pattern,
                // we must remove the text message to avoid the function result showing up in the user output.
                var chatMessage = msg.ToChatMessage();

                if (chatMessage.Role == ChatRole.Tool
                    && chatMessage.Contents.Count == 2
                    && chatMessage.Contents[0] is MEAI.TextContent textContent
                    && chatMessage.Contents[1] is MEAI.FunctionResultContent functionResultContent
                    && textContent.Text == functionResultContent.Result?.ToString())
                {
                    chatMessage.Contents.RemoveAt(0);
                }

                responseMessages.Add(chatMessage);
                return Task.CompletedTask;
            }
        };

        AgentResponseItem<ChatMessageContent>? lastResponseItem = null;
        ChatMessage? lastResponseMessage = null;

        await foreach (var responseItem in _innerAgent.InvokeAsync(messages.Select(x => x.ToChatMessageContent()).ToList(),
                typedThread.InnerThread,
                invokeOptions,
                cancellationToken)
            .ConfigureAwait(false))
        {
            lastResponseItem = responseItem;
        }

        return new MAAI.AgentRunResponse(responseMessages)
        {
            AgentId = _innerAgent.Id,
            RawRepresentation = lastResponseItem,
            AdditionalProperties = lastResponseMessage?.AdditionalProperties,
            CreatedAt = lastResponseMessage?.CreatedAt
        };
    }


    /// <inheritdoc />
    public override async IAsyncEnumerable<MAAI.AgentRunResponseUpdate> RunStreamingAsync(
        IEnumerable<ChatMessage> messages,
        MAAI.AgentThread? thread = null,
        MAAI.AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        thread ??= GetNewThread();

        if (thread is not SemanticKernelAIAgentThread typedThread)
        {
            throw new InvalidOperationException("The provided thread is not compatible with the agent. Only threads created by the agent can be used.");
        }

        await foreach (var responseItem in _innerAgent.InvokeStreamingAsync(messages.Select(x => x.ToChatMessageContent()).ToList(), typedThread.InnerThread, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            var update = responseItem.Message.ToChatResponseUpdate();

            yield return new MAAI.AgentRunResponseUpdate
            {
                AuthorName = update.AuthorName,
                AgentId = _innerAgent.Id,
                RawRepresentation = responseItem,
                AdditionalProperties = update.AdditionalProperties,
                MessageId = update.MessageId,
                Role = update.Role,
                ResponseId = update.ResponseId,
                CreatedAt = update.CreatedAt,
                Contents = update.Contents
            };
        }
    }


    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        Throw.IfNull(serviceType);

        return serviceKey is null && serviceType == typeof(Kernel)
            ? _innerAgent.Kernel
            : serviceKey is null && serviceType.IsInstanceOfType(_innerAgent)
                ? _innerAgent
                : base.GetService(serviceType, serviceKey);
    }
}
