// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Core;
using Moq;
using Moq.Protected;
using Xunit;

namespace SemanticKernel.Plugins.UnitTests.Core;

public sealed class HttpPluginTests : IDisposable
{
    private readonly string _content = "hello world";
    private readonly string _uriString = "http://www.example.com";

    private readonly HttpResponseMessage _response = new()
    {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent("hello world")
    };


    [Fact]
    public void ItCanBeInstantiated()
    {
        // Act - Assert no exception occurs
        var plugin = new HttpPlugin();
    }


    [Fact]
    public void ItCanBeImported()
    {
        // Act - Assert no exception occurs e.g. due to reflection
        Assert.NotNull(KernelPluginFactory.CreateFromType<HttpPlugin>("http"));
    }


    [Fact]
    public async Task ItCanGetAsync()
    {
        // Arrange
        var mockHandler = CreateMock();
        using var client = new HttpClient(mockHandler.Object);
        var plugin = new HttpPlugin(client) { AllowedDomains = ["www.example.com"] };

        // Act
        var result = await plugin.GetAsync(_uriString);

        // Assert
        Assert.Equal(_content, result);
        VerifyMock(mockHandler, HttpMethod.Get);
    }


    [Fact]
    public async Task ItCanPostAsync()
    {
        // Arrange
        var mockHandler = CreateMock();
        using var client = new HttpClient(mockHandler.Object);
        var plugin = new HttpPlugin(client) { AllowedDomains = ["www.example.com"] };

        // Act
        var result = await plugin.PostAsync(_uriString, _content);

        // Assert
        Assert.Equal(_content, result);
        VerifyMock(mockHandler, HttpMethod.Post);
    }


    [Fact]
    public async Task ItCanPutAsync()
    {
        // Arrange
        var mockHandler = CreateMock();
        using var client = new HttpClient(mockHandler.Object);
        var plugin = new HttpPlugin(client) { AllowedDomains = ["www.example.com"] };

        // Act
        var result = await plugin.PutAsync(_uriString, _content);

        // Assert
        Assert.Equal(_content, result);
        VerifyMock(mockHandler, HttpMethod.Put);
    }


    [Fact]
    public async Task ItCanDeleteAsync()
    {
        // Arrange
        var mockHandler = CreateMock();
        using var client = new HttpClient(mockHandler.Object);
        var plugin = new HttpPlugin(client) { AllowedDomains = ["www.example.com"] };

        // Act
        var result = await plugin.DeleteAsync(_uriString);

        // Assert
        Assert.Equal(_content, result);
        VerifyMock(mockHandler, HttpMethod.Delete);
    }


    [Fact]
    public async Task ItDeniesAllDomainsWithDefaultConfigAsync()
    {
        // Arrange
        var mockHandler = CreateMock();
        using var client = new HttpClient(mockHandler.Object);
        var plugin = new HttpPlugin(client);

        // Act & Assert - default config denies all domains
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await plugin.GetAsync(_uriString));
    }


    [Fact]
    public async Task ItThrowsInvalidOperationExceptionForInvalidDomainAsync()
    {
        // Arrange
        var mockHandler = CreateMock();
        using var client = new HttpClient(mockHandler.Object);
        var plugin = new HttpPlugin(client)
        {
            AllowedDomains = ["www.example.com"]
        };
        var invalidUri = "http://www.notexample.com";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await plugin.GetAsync(invalidUri));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await plugin.PostAsync(invalidUri, _content));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await plugin.PutAsync(invalidUri, _content));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await plugin.DeleteAsync(invalidUri));
    }


    private Mock<HttpMessageHandler> CreateMock()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(_response);
        return mockHandler;
    }


    private void VerifyMock(Mock<HttpMessageHandler> mockHandler, HttpMethod method)
    {
        mockHandler.Protected()
            .Verify(
                "SendAsync",
                Times.Exactly(1), // we expected a single external request
                ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == method // we expected a POST request
                        && req.RequestUri == new Uri(_uriString) // to this uri
                ),
                ItExpr.IsAny<CancellationToken>()
            );
    }


    public void Dispose()
    {
        _response.Dispose();
    }
}
