// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Arguments.Extensions;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using Xunit;

namespace SemanticKernel.Agents.UnitTests;

/// <summary>
/// Unit tests for the <see cref="Agent"/> class.
/// </summary>
public class AgentTests
{
    private readonly Mock<Agent> _agentMock;
    private readonly Mock<AgentThread> _agentThreadMock;
    private readonly List<AgentResponseItem<ChatMessageContent>> _invokeResponses = [];
    private readonly List<AgentResponseItem<StreamingChatMessageContent>> _invokeStreamingResponses = [];


    /// <summary>
    /// Initializes a new instance of the <see cref="AgentTests"/> class.
    /// </summary>
    public AgentTests()
    {
        _agentThreadMock = new Mock<AgentThread>(MockBehavior.Strict);

        _invokeResponses.Add(new AgentResponseItem<ChatMessageContent>(new ChatMessageContent(AuthorRole.Assistant, "Hi"), _agentThreadMock.Object));
        _invokeStreamingResponses.Add(new AgentResponseItem<StreamingChatMessageContent>(new StreamingChatMessageContent(AuthorRole.Assistant, "Hi"), _agentThreadMock.Object));

        _agentMock = new Mock<Agent> { CallBase = true };
        _agentMock
            .Setup(x => x.InvokeAsync(
                It.IsAny<ICollection<ChatMessageContent>>(),
                _agentThreadMock.Object,
                It.IsAny<AgentInvokeOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(_invokeResponses.ToAsyncEnumerable());
        _agentMock
            .Setup(x => x.InvokeStreamingAsync(
                It.IsAny<ICollection<ChatMessageContent>>(),
                _agentThreadMock.Object,
                It.IsAny<AgentInvokeOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(_invokeStreamingResponses.ToAsyncEnumerable());
    }


    /// <summary>
    /// Tests that invoking without a message calls the mocked invoke method with an empty array.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task InvokeWithoutMessageCallsMockedInvokeWithEmptyArrayAsync()
    {
        // Arrange
        var options = new AgentInvokeOptions();
        var cancellationToken = new CancellationToken();

        // Act
        await foreach (var response in _agentMock.Object.InvokeAsync(_agentThreadMock.Object, options, cancellationToken))
        {
            // Assert
            Assert.Contains(response, _invokeResponses);
        }

        // Verify that the mocked method was called with the expected parameters
        _agentMock.Verify(
            x => x.InvokeAsync(
                It.Is<ICollection<ChatMessageContent>>(messages => messages.Count == 0),
                _agentThreadMock.Object,
                options,
                cancellationToken),
            Times.Once);
    }


    /// <summary>
    /// Tests that invoking with a string message calls the mocked invoke method with the message in the ICollection of messages.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task InvokeWithStringMessageCallsMockedInvokeWithMessageInCollectionAsync()
    {
        // Arrange
        var message = "Hello, Agent!";
        var options = new AgentInvokeOptions();
        var cancellationToken = new CancellationToken();

        // Act
        await foreach (var response in _agentMock.Object.InvokeAsync(message,
            _agentThreadMock.Object,
            options,
            cancellationToken))
        {
            // Assert
            Assert.Contains(response, _invokeResponses);
        }

        // Verify that the mocked method was called with the expected parameters
        _agentMock.Verify(
            x => x.InvokeAsync(
                It.Is<ICollection<ChatMessageContent>>(messages => messages.Count == 1 && messages.First().Content == message),
                _agentThreadMock.Object,
                options,
                cancellationToken),
            Times.Once);
    }


    /// <summary>
    /// Tests that invoking with a single message calls the mocked invoke method with the message in the ICollection of messages.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task InvokeWithSingleMessageCallsMockedInvokeWithMessageInCollectionAsync()
    {
        // Arrange
        var message = new ChatMessageContent(AuthorRole.User, "Hello, Agent!");
        var options = new AgentInvokeOptions();
        var cancellationToken = new CancellationToken();

        // Act
        await foreach (var response in _agentMock.Object.InvokeAsync(message,
            _agentThreadMock.Object,
            options,
            cancellationToken))
        {
            // Assert
            Assert.Contains(response, _invokeResponses);
        }

        // Verify that the mocked method was called with the expected parameters
        _agentMock.Verify(
            x => x.InvokeAsync(
                It.Is<ICollection<ChatMessageContent>>(messages => messages.Count == 1 && messages.First() == message),
                _agentThreadMock.Object,
                options,
                cancellationToken),
            Times.Once);
    }


    /// <summary>
    /// Tests that invoking streaming without a message calls the mocked invoke method with an empty array.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task InvokeStreamingWithoutMessageCallsMockedInvokeWithEmptyArrayAsync()
    {
        // Arrange
        var options = new AgentInvokeOptions();
        var cancellationToken = new CancellationToken();

        // Act
        await foreach (var response in _agentMock.Object.InvokeStreamingAsync(_agentThreadMock.Object, options, cancellationToken))
        {
            // Assert
            Assert.Contains(response, _invokeStreamingResponses);
        }

        // Verify that the mocked method was called with the expected parameters
        _agentMock.Verify(
            x => x.InvokeStreamingAsync(
                It.Is<ICollection<ChatMessageContent>>(messages => messages.Count == 0),
                _agentThreadMock.Object,
                options,
                cancellationToken),
            Times.Once);
    }


    /// <summary>
    /// Tests that invoking streaming with a string message calls the mocked invoke method with the message in the ICollection of messages.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task InvokeStreamingWithStringMessageCallsMockedInvokeWithMessageInCollectionAsync()
    {
        // Arrange
        var message = "Hello, Agent!";
        var options = new AgentInvokeOptions();
        var cancellationToken = new CancellationToken();

        // Act
        await foreach (var response in _agentMock.Object.InvokeStreamingAsync(message,
            _agentThreadMock.Object,
            options,
            cancellationToken))
        {
            // Assert
            Assert.Contains(response, _invokeStreamingResponses);
        }

        // Verify that the mocked method was called with the expected parameters
        _agentMock.Verify(
            x => x.InvokeStreamingAsync(
                It.Is<ICollection<ChatMessageContent>>(messages => messages.Count == 1 && messages.First().Content == message),
                _agentThreadMock.Object,
                options,
                cancellationToken),
            Times.Once);
    }


    /// <summary>
    /// Tests that invoking streaming with a single message calls the mocked invoke method with the message in the ICollection of messages.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task InvokeStreamingWithSingleMessageCallsMockedInvokeWithMessageInCollectionAsync()
    {
        // Arrange
        var message = new ChatMessageContent(AuthorRole.User, "Hello, Agent!");
        var options = new AgentInvokeOptions();
        var cancellationToken = new CancellationToken();

        // Act
        await foreach (var response in _agentMock.Object.InvokeStreamingAsync(message,
            _agentThreadMock.Object,
            options,
            cancellationToken))
        {
            // Assert
            Assert.Contains(response, _invokeStreamingResponses);
        }

        // Verify that the mocked method was called with the expected parameters
        _agentMock.Verify(
            x => x.InvokeStreamingAsync(
                It.Is<ICollection<ChatMessageContent>>(messages => messages.Count == 1 && messages.First() == message),
                _agentThreadMock.Object,
                options,
                cancellationToken),
            Times.Once);
    }


    /// <summary>
    /// Verify ability to merge null <see cref="KernelArguments"/>.
    /// </summary>
    [Fact]
    public void VerifyNullArgumentMergeWhenRenderingPrompt()
    {
        // Arrange
        KernelArguments? primaryArguments = null;
        // Act
        KernelArguments arguments = primaryArguments.Merge(null);
        // Assert
        Assert.Empty(arguments);

        // Arrange
        KernelArguments overrideArguments = new() { { "test", 1 } };
        // Act
        arguments = primaryArguments.Merge(overrideArguments);
        // Assert
        Assert.StrictEqual(1, arguments.Count);
    }


    /// <summary>
    /// Verify ability to merge <see cref="KernelArguments"/> parameters.
    /// </summary>
    [Fact]
    public void VerifyArgumentParameterMerge()
    {
        // Arrange
        KernelArguments? primaryArguments = new() { { "a", 1 } };
        KernelArguments overrideArguments = new() { { "b", 2 } };

        // Act
        KernelArguments? arguments = primaryArguments.Merge(overrideArguments);

        // Assert
        Assert.NotNull(arguments);
        Assert.Equal(2, arguments.Count);
        Assert.Equal(1, arguments["a"]);
        Assert.Equal(2, arguments["b"]);

        // Arrange
        overrideArguments["a"] = 11;
        overrideArguments["c"] = 3;

        // Act
        arguments = primaryArguments.Merge(overrideArguments);

        // Assert
        Assert.NotNull(arguments);
        Assert.Equal(3, arguments.Count);
        Assert.Equal(11, arguments["a"]);
        Assert.Equal(2, arguments["b"]);
        Assert.Equal(3, arguments["c"]);
    }


    /// <summary>
    /// Verify ability to merge <see cref="KernelArguments.ExecutionSettings"/>.
    /// </summary>
    [Fact]
    public void VerifyArgumentSettingsMerge()
    {
        // Arrange
        FunctionChoiceBehavior autoInvoke = FunctionChoiceBehavior.Auto();
        PromptExecutionSettings promptExecutionSettings = new() { FunctionChoiceBehavior = autoInvoke };
        KernelArguments primaryArgument = new() { ExecutionSettings = new Dictionary<string, PromptExecutionSettings> { { PromptExecutionSettings.DefaultServiceId, promptExecutionSettings } } };
        KernelArguments overrideArgumentsNoSettings = [];

        // Act
        KernelArguments? arguments = primaryArgument.Merge(overrideArgumentsNoSettings);

        // Assert
        Assert.NotNull(arguments);
        Assert.NotNull(arguments.ExecutionSettings);
        Assert.Single(arguments.ExecutionSettings);
        Assert.StrictEqual(autoInvoke, arguments.ExecutionSettings.First().Value.FunctionChoiceBehavior);

        // Arrange
        FunctionChoiceBehavior noInvoke = FunctionChoiceBehavior.None();
        KernelArguments overrideArgumentsWithSettings = new(new PromptExecutionSettings { FunctionChoiceBehavior = noInvoke });

        // Act
        arguments = primaryArgument.Merge(overrideArgumentsWithSettings);

        // Assert
        Assert.NotNull(arguments);
        Assert.NotNull(arguments.ExecutionSettings);
        Assert.Single(arguments.ExecutionSettings);
        Assert.StrictEqual(noInvoke, arguments.ExecutionSettings.First().Value.FunctionChoiceBehavior);
    }
}
