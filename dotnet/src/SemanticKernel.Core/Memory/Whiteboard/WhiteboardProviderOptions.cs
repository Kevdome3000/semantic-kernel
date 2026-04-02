// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SemanticKernel.Memory;

/// <summary>
/// Options for configuring the <see cref="WhiteboardProvider"/>.
/// </summary>
public sealed class WhiteboardProviderOptions
{
    /// <summary>
    /// Gets or sets the maximum number of messages to keep on the whiteboard.
    /// </summary>
    /// <value>
    /// Defaults to 10 if not specified.
    /// </value>
    public int? MaxWhiteboardMessages { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of whiteboard entries to maintain.
    /// Must be greater than 0. Default is 10.
    /// </summary>
    public int MaxWhiteboardEntries { get; set; } = 10;

    /// <summary>
    /// Gets or sets the number of queued messages before forcing a coalescence update.
    /// Default is 3.
    /// </summary>
    public int MaxQueuedMessagesBeforeCoalesce { get; set; } = 3;

    /// <summary>
    /// Gets or sets a string that is prefixed to the messages on the whiteboard,
    /// when providing them as context to the AI model.
    /// </summary>
    /// <value>
    /// Defaults to &quot;## Whiteboard\nThe following list of messages are currently on the whiteboard:&quot;
    /// </value>
    public string? ContextPrompt { get; init; }

    /// <summary>
    /// Gets or sets the message to provide to the AI model when there are no messages on the whiteboard.
    /// </summary>
    /// <value>
    /// Defaults to &quot;## Whiteboard\nThe whiteboard is currently empty.&quot;
    /// </value>
    public string? WhiteboardEmptyPrompt { get; init; }

    /// <summary>
    /// Gets or sets a prompt template to use to update the whiteboard with the latest messages
    /// if the built-in prompt needs to be customized.
    /// </summary>
    /// <remarks>
    /// The following parameters can be used in the prompt:
    /// {{$maxWhiteboardMessages}}
    /// {{$inputMessages}}
    /// {{$currentWhiteboard}}
    /// </remarks>
    public string? MaintenancePromptTemplate { get; init; }
}
