// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel;

using AI;
using Connectors.AI.HuggingFace.TextCompletion;


/// <summary>
/// Provides an extension method for working with Hugging Face model results.
/// </summary>
public static class HuggingFaceModelResultExtension
{
    /// <summary>
    /// Retrieves a typed <see cref="TextCompletionResponse"/> hugging face result from <see cref="ModelResult"/>.
    /// </summary>
    /// <param name="resultBase">The <see cref="ModelResult"/> instance to retrieve the hugging face result from.</param>
    /// <returns>A <see cref="TextCompletionResponse"/> instance containing the hugging face result.</returns>
    public static TextCompletionResponse GetHuggingFaceResult(this ModelResult resultBase)
    {
        return resultBase.GetResult<TextCompletionResponse>();
    }
}
