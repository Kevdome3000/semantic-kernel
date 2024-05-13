// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Embeddings;

/// <summary>
/// Represents a generator of text embeddings of type <c>float</c>.
/// </summary>
public interface ITextEmbeddingGenerationService : IEmbeddingGenerationService<string, float>;
