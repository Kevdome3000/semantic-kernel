// Copyright (c) Microsoft. All rights reserved.

namespace SemanticKernel.IntegrationTests.Connectors.OpenAI;

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextToImage;
using TestSettings;
using Xunit;


public sealed class OpenAITextToImageTests
{

    private readonly IConfigurationRoot _configuration = new ConfigurationBuilder().AddJsonFile(path: "testsettings.json", optional: false, reloadOnChange: true).
        AddJsonFile(path: "testsettings.development.json", optional: true, reloadOnChange: true).
        AddEnvironmentVariables().
        AddUserSecrets<OpenAITextToAudioTests>().
        Build();


    [Fact(Skip = "This test is for manual verification.")]
    public async Task OpenAITextToImageTestAsync()
    {
        // Arrange
        OpenAIConfiguration? openAIConfiguration = this._configuration.GetSection("OpenAITextToImage").
            Get<OpenAIConfiguration>();

        Assert.NotNull(openAIConfiguration);

        var kernel = Kernel.CreateBuilder().
            AddOpenAITextToImage(apiKey: openAIConfiguration.ApiKey).
            Build();

        var service = kernel.GetRequiredService<ITextToImageService>();

        // Act
        var result = await service.GenerateImageAsync("The sun rises in the east and sets in the west.", 512, 512);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }


    [Fact(Skip = "This test is for manual verification.")]
    public async Task OpenAITextToImageByModelTestAsync()
    {
        // Arrange
        OpenAIConfiguration? openAIConfiguration = this._configuration.GetSection("OpenAITextToImage").
            Get<OpenAIConfiguration>();

        Assert.NotNull(openAIConfiguration);

        var kernel = Kernel.CreateBuilder().
            AddOpenAITextToImage(apiKey: openAIConfiguration.ApiKey, modelId: openAIConfiguration.ModelId).
            Build();

        var service = kernel.GetRequiredService<ITextToImageService>();

        // Act
        var result = await service.GenerateImageAsync("The sun rises in the east and sets in the west.", 1024, 1024);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }


    [Fact(Skip = "This test is for manual verification.")]
    public async Task AzureOpenAITextToImageTestAsync()
    {
        // Arrange
        AzureOpenAIConfiguration? azureOpenAIConfiguration = this._configuration.GetSection("AzureOpenAITextToImage").
            Get<AzureOpenAIConfiguration>();

        Assert.NotNull(azureOpenAIConfiguration);

        var kernel = Kernel.CreateBuilder().
            AddAzureOpenAITextToImage(
                azureOpenAIConfiguration.DeploymentName,
                azureOpenAIConfiguration.Endpoint,
                azureOpenAIConfiguration.ApiKey).
            Build();

        var service = kernel.GetRequiredService<ITextToImageService>();

        // Act
        var result = await service.GenerateImageAsync("The sun rises in the east and sets in the west.", 1024, 1024);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

}
