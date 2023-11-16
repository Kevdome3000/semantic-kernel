// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Experimental.Assistants;

using System.Text.Json.Serialization;


/// <summary>
/// Response from assistant when called as a <see cref="ISKFunction"/>.
/// </summary>
public class AssistantResponse
{
    /// <summary>
    /// The thread-id for the assistant conversation.
    /// </summary>
    [JsonPropertyName("thread_id")]
    public string ThreadId { get; set; } = string.Empty;

    /// <summary>
    /// The assistant response.
    /// </summary>
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Instructions from assistant on next steps.
    /// </summary>
    [JsonPropertyName("system_instructions")]
    public string Instructions { get; set; } = string.Empty;
}
