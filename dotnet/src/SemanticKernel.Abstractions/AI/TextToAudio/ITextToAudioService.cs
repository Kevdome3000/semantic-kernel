// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.TextToAudio;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Services;


/// <summary>
/// Interface for text-to-audio services.
/// </summary>
public interface ITextToAudioService : IAIService
{

    /// <summary>
    /// Get audio contents from text.
    /// </summary>
    /// <param name="text">The text to generate audio for.</param>
    /// <param name="executionSettings">The AI execution settings (optional).</param>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>Audio contents from text.</returns>
    Task<IReadOnlyList<AudioContent>> GetAudioContentsAsync(
        string text,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default);

}
