// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Embeddings;

using System.Diagnostics.CodeAnalysis;


/// <summary>
/// Represents a generator of text embeddings of type <c>float</c>.
/// </summary>
[Experimental("SKEXP0001")]
public interface ITextEmbeddingGeneration : IEmbeddingGeneration<string, float>
{
}
