// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ChatCompletion;
using TextGeneration;


/// <summary>
/// Provides execution settings for an AI request.
/// </summary>
/// <remarks>
/// Implementors of <see cref="ITextGenerationService"/> or <see cref="IChatCompletionService"/> can extend this
/// if the service they are calling supports additional properties. For an example, please reference
/// the Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIPromptExecutionSettings implementation.
/// </remarks>
public class PromptExecutionSettings
{

    /// <summary>
    /// Gets the default service identifier.
    /// </summary>
    /// <remarks>
    /// In a dictionary of <see cref="PromptExecutionSettings"/>, this is the key that should be used settings considered the default.
    /// </remarks>
    public static string DefaultServiceId => "default";

    /// <summary>
    /// Model identifier.
    /// This identifies the AI model these settings are configured for e.g., gpt-4, gpt-3.5-turbo
    /// </summary>
    [JsonPropertyName("model_id")]
    public string? ModelId
    {
        get => _modelId;

        set
        {
            ThrowIfFrozen();
            _modelId = value;
        }
    }

    /// <summary>
    /// Extra properties that may be included in the serialized execution settings.
    /// </summary>
    /// <remarks>
    /// Avoid using this property if possible. Instead, use one of the classes that extends <see cref="PromptExecutionSettings"/>.
    /// </remarks>
    [JsonExtensionData]
    [JsonIgnore]
    public IDictionary<string, object>? ExtensionData
    {
        get => _extensionData;

        set
        {
            ThrowIfFrozen();
            _extensionData = value;
        }
    }

    /// <summary>
    /// Gets a value that indicates whether the <see cref="PromptExecutionSettings"/> are currently modifiable.
    /// </summary>
    [JsonIgnore]
    public bool IsFrozen { get; protected set; }


    /// <summary>
    /// Makes the current <see cref="PromptExecutionSettings"/> unmodifiable and sets its IsFrozen property to true.
    /// </summary>
    public virtual void Freeze()
    {
        if (IsFrozen)
        {
            return;
        }

        IsFrozen = true;

        if (_extensionData is not null)
        {
            _extensionData = new ReadOnlyDictionary<string, object>(_extensionData);
        }
    }


    /// <summary>
    /// Creates a new <see cref="PromptExecutionSettings"/> object that is a copy of the current instance.
    /// </summary>
    public virtual PromptExecutionSettings Clone() => new()
    {
        ModelId = ModelId,
        ExtensionData = ExtensionData is not null
            ? new Dictionary<string, object>(ExtensionData)
            : null
    };


    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if the <see cref="PromptExecutionSettings"/> are frozen.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    protected void ThrowIfFrozen()
    {
        if (IsFrozen)
        {
            throw new InvalidOperationException("PromptExecutionSettings are frozen and cannot be modified.");
        }
    }


    #region private ================================================================================

    private string? _modelId;

    private IDictionary<string, object>? _extensionData;

    #endregion


}
