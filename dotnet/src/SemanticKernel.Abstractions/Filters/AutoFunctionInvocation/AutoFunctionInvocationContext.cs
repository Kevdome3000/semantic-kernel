// Copyright (c) Microsoft.All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Microsoft.SemanticKernel;

/// <summary>
/// Class with data related to automatic function invocation.
/// </summary>
public class AutoFunctionInvocationContext : Microsoft.Extensions.AI.FunctionInvocationContext
{
    private ChatHistory? _chatHistory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoFunctionInvocationContext"/> class from an existing <see cref="Microsoft.Extensions.AI.FunctionInvocationContext"/>.
    /// </summary>
    internal AutoFunctionInvocationContext(KernelChatOptions autoInvocationChatOptions, AIFunction aiFunction)
    {
        Verify.NotNull(autoInvocationChatOptions);
        Verify.NotNull(aiFunction);
        if (aiFunction is not KernelFunction kernelFunction)
        {
            throw new InvalidOperationException($"The function must be of type {nameof(KernelFunction)}.");
        }
        Verify.NotNull(autoInvocationChatOptions.Kernel);
        Verify.NotNull(autoInvocationChatOptions.ChatMessageContent);

        Options = autoInvocationChatOptions;
        ExecutionSettings = autoInvocationChatOptions.ExecutionSettings;
        AIFunction = aiFunction;
        Result = new FunctionResult(kernelFunction) { Culture = autoInvocationChatOptions.Kernel.Culture };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoFunctionInvocationContext"/> class.
    /// </summary>
    /// <param name="kernel">The <see cref="Microsoft.SemanticKernel.Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    /// <param name="function">The <see cref="KernelFunction"/> with which this filter is associated.</param>
    /// <param name="result">The result of the function's invocation.</param>
    /// <param name="chatHistory">The chat history associated with automatic function invocation.</param>
    /// <param name="chatMessageContent">The chat message content associated with automatic function invocation.</param>
    public AutoFunctionInvocationContext(
        Kernel kernel,
        KernelFunction function,
        FunctionResult result,
        ChatHistory chatHistory,
        ChatMessageContent chatMessageContent)
    {
        Verify.NotNull(kernel);
        Verify.NotNull(function);
        Verify.NotNull(result);
        Verify.NotNull(chatHistory);
        Verify.NotNull(chatMessageContent);

        Options = new KernelChatOptions(kernel)
        {
            ChatMessageContent = chatMessageContent,
        };

        _chatHistory = chatHistory;
        Messages = chatHistory.ToChatMessageList();
        chatHistory.SetChatMessageHandlers(Messages);
        base.Function = function;
        Result = result;
    }

    /// <summary>
    /// The <see cref="System.Threading.CancellationToken"/> to monitor for cancellation requests.
    /// The default is <see cref="System.Threading.CancellationToken.None"/>.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Gets the <see cref="KernelArguments"/> specialized version of <see cref="AIFunctionArguments"/> associated with the operation.
    /// </summary>
    /// <remarks>
    /// Due to a clash with the <see cref="Microsoft.Extensions.AI.FunctionInvocationContext.Arguments"/> as a <see cref="AIFunctionArguments"/> type, this property hides
    /// it to not break existing code that relies on the <see cref="AutoFunctionInvocationContext.Arguments"/> as a <see cref="KernelArguments"/> type.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Attempting to access the property when the arguments is not a <see cref="KernelArguments"/> class.</exception>
    public new KernelArguments? Arguments
    {
        get
        {
            if (base.Arguments is KernelArguments kernelArguments)
            {
                return kernelArguments;
            }

            throw new InvalidOperationException($"The arguments provided in the initialization must be of type {nameof(KernelArguments)}.");
        }
        init => base.Arguments = value ?? new();
    }

    /// <summary>
    /// Request sequence index of automatic function invocation process. Starts from 0.
    /// </summary>
    public int RequestSequenceIndex
    {
        get => Iteration;
        init => Iteration = value;
    }

    /// <summary>
    /// Function sequence index. Starts from 0.
    /// </summary>
    public int FunctionSequenceIndex
    {
        get => FunctionCallIndex;
        init => FunctionCallIndex = value;
    }

    /// <summary>
    /// The ID of the tool call.
    /// </summary>
    public string? ToolCallId
    {
        get => CallContent.CallId;
        init
        {
            CallContent = new Microsoft.Extensions.AI.FunctionCallContent(
                callId: value ?? string.Empty,
                name: CallContent.Name,
                arguments: CallContent.Arguments);
        }
    }

    /// <summary>
    /// The chat message content associated with automatic function invocation.
    /// </summary>
    public ChatMessageContent ChatMessageContent => (Options as KernelChatOptions)!.ChatMessageContent!;

    /// <summary>
    /// The execution settings associated with the operation.
    /// </summary>
    public PromptExecutionSettings? ExecutionSettings
    {
        get => ((KernelChatOptions)Options!).ExecutionSettings;
        init
        {
            Options ??= new KernelChatOptions(Kernel);
            ((KernelChatOptions)Options!).ExecutionSettings = value;
        }
    }

    /// <summary>
    /// Gets the <see cref="Microsoft.SemanticKernel.ChatCompletion.ChatHistory"/> associated with automatic function invocation.
    /// </summary>
    public ChatHistory ChatHistory => _chatHistory ??= new ChatMessageHistory(Messages);

    /// <summary>
    /// Gets the <see cref="KernelFunction"/> with which this filter is associated.
    /// </summary>
    /// <para>
    /// Due to a clash with the <see cref="Microsoft.Extensions.AI.FunctionInvocationContext.Function"/> as a <see cref="AIFunction"/> type, this property hides
    /// it to not break existing code that relies on the <see cref="AutoFunctionInvocationContext.Function"/> as a <see cref="KernelFunction"/> type.
    /// </para>
    public new KernelFunction Function
    {
        get
        {
            if (AIFunction is KernelFunction kf)
            {
                return kf;
            }

            throw new InvalidOperationException($"The function provided in the initialization must be of type {nameof(KernelFunction)}.");
        }
    }

    /// <summary>
    /// Gets the <see cref="Microsoft.SemanticKernel.Kernel"/> containing services, plugins, and other state for use throughout the operation.
    /// </summary>
    public Kernel Kernel => ((KernelChatOptions)Options!).Kernel!;

    /// <summary>
    /// Gets or sets the result of the function's invocation.
    /// </summary>
    public FunctionResult Result { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Microsoft.Extensions.AI.AIFunction"/> with which this filter is associated.
    /// </summary>
    internal AIFunction AIFunction
    {
        get => base.Function;
        set => base.Function = value;
    }

    private static bool IsSameSchema(KernelFunction kernelFunction, AIFunction aiFunction)
    {
        // Compares the schemas, should be similar.
        return string.Equals(
            kernelFunction.JsonSchema.ToString(),
            aiFunction.JsonSchema.ToString(),
            StringComparison.OrdinalIgnoreCase);

        // TODO: Later can be improved by comparing the underlying methods.
        // return kernelFunction.UnderlyingMethod == aiFunction.UnderlyingMethod;
    }

    /// <summary>
    /// Mutable IEnumerable of chat message as chat history.
    /// </summary>
    private class ChatMessageHistory : ChatHistory, IEnumerable<ChatMessageContent>
    {
        private readonly List<ChatMessage> _messages;

        internal ChatMessageHistory(IEnumerable<ChatMessage> messages) : base(messages.ToChatHistory())
        {
            _messages = new List<ChatMessage>(messages);
        }

        public override void Add(ChatMessageContent item)
        {
            base.Add(item);
            _messages.Add(item.ToChatMessage());
        }

        public override void Clear()
        {
            base.Clear();
            _messages.Clear();
        }

        public override bool Remove(ChatMessageContent item)
        {
            var index = base.IndexOf(item);

            if (index < 0)
            {
                return false;
            }

            _messages.RemoveAt(index);
            base.RemoveAt(index);

            return true;
        }

        public override void Insert(int index, ChatMessageContent item)
        {
            base.Insert(index, item);
            _messages.Insert(index, item.ToChatMessage());
        }

        public override void RemoveAt(int index)
        {
            _messages.RemoveAt(index);
            base.RemoveAt(index);
        }

        public override ChatMessageContent this[int index]
        {
            get => _messages[index].ToChatMessageContent();
            set
            {
                _messages[index] = value.ToChatMessage();
                base[index] = value;
            }
        }

        public override void RemoveRange(int index, int count)
        {
            _messages.RemoveRange(index, count);
            base.RemoveRange(index, count);
        }

        public override void CopyTo(ChatMessageContent[] array, int arrayIndex)
        {
            for (int i = 0; i < _messages.Count; i++)
            {
                array[arrayIndex + i] = _messages[i].ToChatMessageContent();
            }
        }

        public override bool Contains(ChatMessageContent item) => base.Contains(item);

        public override int IndexOf(ChatMessageContent item) => base.IndexOf(item);

        public override void AddRange(IEnumerable<ChatMessageContent> items)
        {
            base.AddRange(items);
            _messages.AddRange(items.Select(i => i.ToChatMessage()));
        }

        public override int Count => _messages.Count;

        // Explicit implementation of IEnumerable<ChatMessageContent>.GetEnumerator()
        IEnumerator<ChatMessageContent> IEnumerable<ChatMessageContent>.GetEnumerator()
        {
            foreach (var message in _messages)
            {
                yield return message.ToChatMessageContent(); // Convert and yield each item
            }
        }

        // Explicit implementation of non-generic IEnumerable.GetEnumerator()
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<ChatMessageContent>)this).GetEnumerator();
    }

    /// <summary>Destructor to clear the chat history overrides.</summary>
    ~AutoFunctionInvocationContext()
    {
        // The moment this class is destroyed, we need to clear the update message overrides
        _chatHistory?.ClearOverrides();
    }
}
