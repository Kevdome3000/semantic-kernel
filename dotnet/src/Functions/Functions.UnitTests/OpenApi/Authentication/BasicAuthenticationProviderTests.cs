// Copyright (c) Microsoft. All rights reserved.

namespace SemanticKernel.Functions.UnitTests.OpenApi.Authentication;

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Plugins.OpenApi.Authentication;
using Xunit;


public class BasicAuthenticationProviderTests
{
    [Fact]
    public async Task AuthenticateRequestAsyncSucceedsAsync()
    {
        // Arrange
        var credentials = Guid.NewGuid().ToString();
        using var request = new HttpRequestMessage();

        var target = new BasicAuthenticationProvider(() => Task.FromResult(credentials));

        // Act
        await target.AuthenticateRequestAsync(request);

        // Assert
        Assert.Equal("Basic", request.Headers.Authorization?.Scheme);
        Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)), request.Headers.Authorization?.Parameter);
    }
}
