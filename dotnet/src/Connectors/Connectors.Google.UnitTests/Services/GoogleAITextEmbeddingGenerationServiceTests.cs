// Copyright (c) Microsoft. All rights reserved.

namespace SemanticKernel.Connectors.Google.UnitTests.Services;

using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Services;
using Xunit;


public sealed class GoogleAITextEmbeddingGenerationServiceTests
{

    [Fact]
    public void AttributesShouldContainModelId()
    {
        // Arrange & Act
        string model = "fake-model";
        var service = new GoogleAITextEmbeddingGenerationService(model, "key");

        // Assert
        Assert.Equal(model, service.Attributes[AIServiceExtensions.ModelIdKey]);
    }

}
