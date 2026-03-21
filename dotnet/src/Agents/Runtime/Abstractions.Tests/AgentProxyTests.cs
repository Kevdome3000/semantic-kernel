// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.SemanticKernel.Agents.Runtime.Abstractions.Tests;

[Trait("Category", "Unit")]
public class AgentProxyTests
{
    private readonly Mock<IAgentRuntime> mockRuntime;
    private readonly AgentId agentId;
    private readonly AgentProxy agentProxy;


    public AgentProxyTests()
    {
        mockRuntime = new Mock<IAgentRuntime>();
        agentId = new AgentId("testType", "testKey");
        agentProxy = new AgentProxy(agentId, mockRuntime.Object);
    }


    [Fact]
    public void IdMatchesAgentIdTest()
    {
        // Assert
        Assert.Equal(agentId, agentProxy.Id);
    }


    [Fact]
    public void MetadataShouldMatchAgentTest()
    {
        AgentMetadata expectedMetadata = new("testType", "testKey", "testDescription");
        mockRuntime.Setup(r => r.GetAgentMetadataAsync(agentId))
            .ReturnsAsync(expectedMetadata);

        Assert.Equal(expectedMetadata, agentProxy.Metadata);
    }


    [Fact]
    public async Task SendMessageResponseTest()
    {
        // Arrange
        object message = new { Content = "Hello" };
        AgentId sender = new("senderType", "senderKey");
        object response = new { Content = "Response" };

        mockRuntime.Setup(r => r.SendMessageAsync(message,
                agentId,
                sender,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        object? result = await agentProxy.SendMessageAsync(message, sender);

        // Assert
        Assert.Equal(response, result);
    }


    [Fact]
    public async Task LoadStateTest()
    {
        // Arrange
        JsonElement state = JsonElement.Parse("{\"key\":\"value\"}");

        mockRuntime.Setup(r => r.LoadAgentStateAsync(agentId, state))
            .Returns(ValueTask.CompletedTask);

        // Act
        await agentProxy.LoadStateAsync(state);

        // Assert
        mockRuntime.Verify(r => r.LoadAgentStateAsync(agentId, state), Times.Once);
    }


    [Fact]
    public async Task SaveStateTest()
    {
        // Arrange
        JsonElement expectedState = JsonElement.Parse("{\"key\":\"value\"}");

        mockRuntime.Setup(r => r.SaveAgentStateAsync(agentId))
            .ReturnsAsync(expectedState);

        // Act
        JsonElement result = await agentProxy.SaveStateAsync();

        // Assert
        Assert.Equal(expectedState, result);
    }
}
