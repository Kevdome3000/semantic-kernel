﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel;

using System.Net.Http;
using ChatCompletion;
using Connectors.Google;
using Embeddings;
using Extensions.DependencyInjection;
using Extensions.Logging;
using Http;


/// <summary>
/// Extensions for adding GoogleAI generation services to the application.
/// </summary>
public static class GoogleAIKernelBuilderExtensions
{

    /// <summary>
    /// Add Google AI Gemini Chat Completion and Text Generation services to the kernel builder.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="modelId">The model for text generation.</param>
    /// <param name="apiKey">The API key for authentication Gemini API.</param>
    /// <param name="apiVersion">The version of the Google API.</param>
    /// <param name="serviceId">The optional service ID.</param>
    /// <param name="httpClient">The optional custom HttpClient.</param>
    /// <returns>The updated kernel builder.</returns>
    public static IKernelBuilder AddGoogleAIGeminiChatCompletion(
        this IKernelBuilder builder,
        string modelId,
        string apiKey,
        GoogleAIVersion apiVersion = GoogleAIVersion.V1_Beta, // todo: change beta to stable when stable version will be available
        string? serviceId = null,
        HttpClient? httpClient = null)
    {
        Verify.NotNull(builder);
        Verify.NotNull(modelId);
        Verify.NotNull(apiKey);

        builder.Services.AddKeyedSingleton<IChatCompletionService>(serviceId, (serviceProvider, _) =>
            new GoogleAIGeminiChatCompletionService(
                modelId: modelId,
                apiKey: apiKey,
                apiVersion: apiVersion,
                httpClient: HttpClientProvider.GetHttpClient(httpClient, serviceProvider),
                loggerFactory: serviceProvider.GetService<ILoggerFactory>()));

        return builder;
    }


    /// <summary>
    /// Add Google AI embeddings generation service to the kernel builder.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="modelId">The model for text generation.</param>
    /// <param name="apiKey">The API key for authentication Gemini API.</param>
    /// <param name="apiVersion">The version of the Google API.</param>
    /// <param name="serviceId">The optional service ID.</param>
    /// <param name="httpClient">The optional custom HttpClient.</param>
    /// <returns>The updated kernel builder.</returns>
    public static IKernelBuilder AddGoogleAIEmbeddingGeneration(
        this IKernelBuilder builder,
        string modelId,
        string apiKey,
        GoogleAIVersion apiVersion = GoogleAIVersion.V1_Beta, // todo: change beta to stable when stable version will be available
        string? serviceId = null,
        HttpClient? httpClient = null)
    {
        Verify.NotNull(builder);
        Verify.NotNull(modelId);
        Verify.NotNull(apiKey);

        builder.Services.AddKeyedSingleton<ITextEmbeddingGenerationService>(serviceId, (serviceProvider, _) =>
            new GoogleAITextEmbeddingGenerationService(
                modelId: modelId,
                apiKey: apiKey,
                apiVersion: apiVersion,
                httpClient: HttpClientProvider.GetHttpClient(httpClient, serviceProvider),
                loggerFactory: serviceProvider.GetService<ILoggerFactory>()));

        return builder;
    }

}
