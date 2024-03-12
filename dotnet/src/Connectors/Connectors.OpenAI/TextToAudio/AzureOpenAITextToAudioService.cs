// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Logging;
using Services;
using TextToAudio;


/// <summary>
/// Azure OpenAI text-to-audio service.
/// </summary>
public sealed class AzureOpenAITextToAudioService : ITextToAudioService
{

    /// <summary>
    /// Azure OpenAI text-to-audio client for HTTP operations.
    /// </summary>
    private readonly AzureOpenAITextToAudioClient _client;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> Attributes => _client.Attributes;

    /// <summary>
    /// Gets the key used to store the deployment name in the <see cref="IAIService.Attributes"/> dictionary.
    /// </summary>
    public static string DeploymentNameKey => "DeploymentName";


    /// <summary>
    /// Creates an instance of the <see cref="AzureOpenAITextToAudioService"/> connector with API key auth.
    /// </summary>
    /// <param name="deploymentName">Azure OpenAI deployment name, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="endpoint">Azure OpenAI deployment URL, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="apiKey">Azure OpenAI API key, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="modelId">Azure OpenAI model id, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="httpClient">Custom <see cref="HttpClient"/> for HTTP requests.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
    public AzureOpenAITextToAudioService(
        string deploymentName,
        string endpoint,
        string apiKey,
        string? modelId = null,
        HttpClient? httpClient = null,
        ILoggerFactory? loggerFactory = null)
    {
        _client = new AzureOpenAITextToAudioClient(deploymentName, endpoint, apiKey, modelId,
            httpClient, loggerFactory?.CreateLogger(typeof(AzureOpenAITextToAudioService)));

        _client.AddAttribute(DeploymentNameKey, deploymentName);
        _client.AddAttribute(AIServiceExtensions.ModelIdKey, modelId);
    }


    /// <inheritdoc/>
    public Task<IReadOnlyList<AudioContent>> GetAudioContentsAsync(
        string text,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
        => _client.GetAudioContentsAsync(text, executionSettings, cancellationToken);

}
