// Copyright (c) Microsoft.All rights reserved.

namespace Microsoft.SemanticKernel.Embeddings;

/// <summary>
/// Represents a generator of text embeddings of type <c>float</c>.
/// </summary>
[Experimental("SKEXP0001")]
[Obsolete("Use Microsoft.Extensions.AI.IEmbeddingGenerator<string, Embedding<float>> instead.")]
public interface ITextEmbeddingGenerationService : IEmbeddingGenerationService<string, float>;
