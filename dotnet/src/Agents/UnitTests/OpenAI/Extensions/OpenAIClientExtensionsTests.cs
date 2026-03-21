// Copyright (c) Microsoft. All rights reserved.
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Agents.OpenAI;
using OpenAI;
using OpenAI.VectorStores;
using Xunit;

namespace SemanticKernel.Agents.UnitTests.OpenAI.Extensions;

/// <summary>
/// Unit testing of <see cref="OpenAIAssistantAgent"/>.
/// </summary>
public sealed class OpenAIClientExtensionsTests : IDisposable
{
    private readonly HttpMessageHandlerStub _messageHandlerStub;
    private readonly HttpClient _httpClient;
    private readonly OpenAIClient _client;


    /// <summary>
    /// Verify the default creation of vector-store.
    /// </summary>
    [Fact]
    public async Task VerifyCreateDefaultVectorStoreAsync()
    {
        // Arrange
        string[] fileIds = ["file-1", "file-2"];
        SetupResponse(HttpStatusCode.OK, OpenAIAssistantResponseContent.CreateVectorStore);

        // Act
        string storeId = await _client.CreateVectorStoreAsync(fileIds);

        // Assert
        Assert.NotNull(storeId);
    }


    /// <summary>
    /// Verify the custom creation of vector-store.
    /// </summary>
    [Fact]
    public async Task VerifyCreateVectorStoreAsync()
    {
        // Arrange
        string[] fileIds = ["file-1", "file-2"];
        Dictionary<string, string> metadata =
            new()
            {
                { "a", "1" },
                { "b", "2" }
            };
        SetupResponse(HttpStatusCode.OK, OpenAIAssistantResponseContent.CreateVectorStore);

        // Act
        string storeId = await _client.CreateVectorStoreAsync(
            fileIds,
            "test-store",
            new VectorStoreExpirationPolicy(VectorStoreExpirationAnchor.LastActiveAt, 30),
            FileChunkingStrategy.Auto,
            metadata);

        // Assert
        Assert.NotNull(storeId);
    }


    /// <summary>
    /// Verify the uploading an assistant file.
    /// </summary>
    [Fact]
    public async Task VerifyUploadFileAsync()
    {
        // Arrange
        SetupResponse(HttpStatusCode.OK, OpenAIAssistantResponseContent.UploadFile);

        // Act
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes("test"));
        string fileId = await _client.UploadAssistantFileAsync(stream, "text.txt");

        // Assert
        Assert.NotNull(fileId);
    }


    /// <summary>
    /// Verify the deleting a file.
    /// </summary>
    [Fact]
    public async Task VerifyDeleteFileAsync()
    {
        // Arrange
        SetupResponse(HttpStatusCode.OK, OpenAIAssistantResponseContent.DeleteFile);

        // Act
        bool isDeleted = await _client.DeleteFileAsync("file-id");

        // Assert
        Assert.True(isDeleted);
    }


    /// <summary>
    /// Verify the deleting a vector-store.
    /// </summary>
    [Fact]
    public async Task VerifyDeleteVectorStoreAsync()
    {
        // Arrange
        SetupResponse(HttpStatusCode.OK, OpenAIAssistantResponseContent.DeleteVectorStore);

        // Act
        bool isDeleted = await _client.DeleteVectorStoreAsync("store-id");

        // Assert
        Assert.True(isDeleted);
    }


    /// <inheritdoc/>
    public void Dispose()
    {
        _messageHandlerStub.Dispose();
        _httpClient.Dispose();
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIAssistantAgentTests"/> class.
    /// </summary>
    public OpenAIClientExtensionsTests()
    {
        _messageHandlerStub = new HttpMessageHandlerStub();
        _httpClient = new HttpClient(_messageHandlerStub, false);
        _client = OpenAIAssistantAgent.CreateOpenAIClient(new ApiKeyCredential("fakekey"), null, _httpClient);
    }


    private void SetupResponse(HttpStatusCode statusCode, string content)
    {
        _messageHandlerStub.SetupResponses(statusCode, content);
    }
}
