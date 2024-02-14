// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.AudioToText;

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Contents;
using Services;


/// <summary>
/// Interface for audio-to-text services.
/// </summary>
[Experimental("SKEXP0005")]
public interface IAudioToTextService : IAIService
{

    /// <summary>
    /// Get text content from audio content.
    /// </summary>
    /// <param name="content">Audio content.</param>
    /// <param name="executionSettings">The AI execution settings (optional).</param>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>Text content from audio content.</returns>
    Task<TextContent> GetTextContentAsync(
        AudioContent content,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default);

}
