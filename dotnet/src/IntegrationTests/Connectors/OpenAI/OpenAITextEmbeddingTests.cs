﻿// Copyright (c) Microsoft. All rights reserved.

namespace SemanticKernel.IntegrationTests.Connectors.OpenAI;

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using TestSettings;
using Xunit;


public sealed class OpenAITextEmbeddingTests
{

    private const int AdaVectorLength = 1536;

    private readonly IConfigurationRoot _configuration = new ConfigurationBuilder().AddJsonFile(path: "testsettings.json", optional: false, reloadOnChange: true).
        AddJsonFile(path: "testsettings.development.json", optional: true, reloadOnChange: true).
        AddEnvironmentVariables().
        AddUserSecrets<OpenAITextEmbeddingTests>().
        Build();


    [Theory(Skip = "OpenAI will often throttle requests. This test is for manual verification.")]
    [InlineData("test sentence")]
    public async Task OpenAITestAsync(string testInputString)
    {
        // Arrange
        OpenAIConfiguration? openAIConfiguration = this._configuration.GetSection("OpenAIEmbeddings").
            Get<OpenAIConfiguration>();

        Assert.NotNull(openAIConfiguration);

        var embeddingGenerator = new OpenAITextEmbeddingGenerationService(openAIConfiguration.ModelId, openAIConfiguration.ApiKey);

        // Act
        var singleResult = await embeddingGenerator.GenerateEmbeddingAsync(testInputString);
        var batchResult = await embeddingGenerator.GenerateEmbeddingsAsync([testInputString, testInputString, testInputString]);

        // Assert
        Assert.Equal(AdaVectorLength, singleResult.Length);
        Assert.Equal(3, batchResult.Count);
    }


    [Theory(Skip = "OpenAI will often throttle requests. This test is for manual verification.")]
    [InlineData(null, 3072)]
    [InlineData(1024, 1024)]
    public async Task OpenAIWithDimensionsAsync(int? dimensions, int expectedVectorLength)
    {
        // Arrange
        const string TestInputString = "test sentence";

        OpenAIConfiguration? openAIConfiguration = this._configuration.GetSection("OpenAIEmbeddings").
            Get<OpenAIConfiguration>();

        Assert.NotNull(openAIConfiguration);

        var embeddingGenerator = new OpenAITextEmbeddingGenerationService(
            "text-embedding-3-large",
            openAIConfiguration.ApiKey,
            dimensions: dimensions);

        // Act
        var result = await embeddingGenerator.GenerateEmbeddingAsync(TestInputString);

        // Assert
        Assert.Equal(expectedVectorLength, result.Length);
    }


    [Theory]
    [InlineData("test sentence")]
    public async Task AzureOpenAITestAsync(string testInputString)
    {
        // Arrange
        AzureOpenAIConfiguration? azureOpenAIConfiguration = this._configuration.GetSection("AzureOpenAIEmbeddings").
            Get<AzureOpenAIConfiguration>();

        Assert.NotNull(azureOpenAIConfiguration);

        var embeddingGenerator = new AzureOpenAITextEmbeddingGenerationService(azureOpenAIConfiguration.DeploymentName,
            azureOpenAIConfiguration.Endpoint,
            azureOpenAIConfiguration.ApiKey);

        // Act
        var singleResult = await embeddingGenerator.GenerateEmbeddingAsync(testInputString);
        var batchResult = await embeddingGenerator.GenerateEmbeddingsAsync([testInputString, testInputString, testInputString]);

        // Assert
        Assert.Equal(AdaVectorLength, singleResult.Length);
        Assert.Equal(3, batchResult.Count);
    }


    [Theory]
    [InlineData(null, 3072)]
    [InlineData(1024, 1024)]
    public async Task AzureOpenAIWithDimensionsAsync(int? dimensions, int expectedVectorLength)
    {
        // Arrange
        const string TestInputString = "test sentence";

        AzureOpenAIConfiguration? azureOpenAIConfiguration = this._configuration.GetSection("AzureOpenAIEmbeddings").
            Get<AzureOpenAIConfiguration>();

        Assert.NotNull(azureOpenAIConfiguration);

        var embeddingGenerator = new AzureOpenAITextEmbeddingGenerationService(
            "text-embedding-3-large",
            azureOpenAIConfiguration.Endpoint,
            azureOpenAIConfiguration.ApiKey,
            dimensions: dimensions);

        // Act
        var result = await embeddingGenerator.GenerateEmbeddingAsync(TestInputString);

        // Assert
        Assert.Equal(expectedVectorLength, result.Length);
    }

}
