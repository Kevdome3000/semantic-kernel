﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel;

using System.IO;
using Connectors.Onnx;
using Embeddings;
using Extensions.DependencyInjection;

#pragma warning disable CA2000 // Dispose objects before losing scope


/// <summary>
/// Provides extension methods for the <see cref="IServiceCollection"/> interface to configure ONNX connectors.
/// </summary>
public static class OnnxServiceCollectionExtensions
{

    /// <summary>Adds a text embedding generation service using a BERT ONNX model.</summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to augment.</param>
    /// <param name="onnxModelPath">The path to the ONNX model file.</param>
    /// <param name="vocabPath">The path to the vocab file.</param>
    /// <param name="options">Options for the configuration of the model and service.</param>
    /// <param name="serviceId">A local identifier for the given AI service.</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    public static IServiceCollection AddBertOnnxTextEmbeddingGeneration(
        this IServiceCollection services,
        string onnxModelPath,
        string vocabPath,
        BertOnnxOptions? options = null,
        string? serviceId = null)
    {
        return services.AddKeyedSingleton<ITextEmbeddingGenerationService>(
            serviceId,
            BertOnnxTextEmbeddingGenerationService.Create(onnxModelPath, vocabPath, options));
    }


    /// <summary>Adds a text embedding generation service using a BERT ONNX model.</summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to augment.</param>
    /// <param name="onnxModelStream">Stream containing the ONNX model. The stream will be read during this call and will not be used after this call's completion.</param>
    /// <param name="vocabStream">Stream containing the vocab file. The stream will be read during this call and will not be used after this call's completion.</param>
    /// <param name="options">Options for the configuration of the model and service.</param>
    /// <param name="serviceId">A local identifier for the given AI service.</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    public static IServiceCollection AddBertOnnxTextEmbeddingGeneration(
        this IServiceCollection services,
        Stream onnxModelStream,
        Stream vocabStream,
        BertOnnxOptions? options = null,
        string? serviceId = null)
    {
        return services.AddKeyedSingleton<ITextEmbeddingGenerationService>(
            serviceId,
            BertOnnxTextEmbeddingGenerationService.Create(onnxModelStream, vocabStream, options));
    }

}
