﻿// Copyright (c) Microsoft.All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;

namespace Microsoft.SemanticKernel;
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
    /// Service identifier.
    /// This identifies the service these settings are configured for e.g., azure_openai_eastus, openai, ollama, huggingface, etc.
    /// </summary>
    /// <remarks>
    /// When provided, this service identifier will be the key in a dictionary collection of execution settings for both <see cref="KernelArguments"/> and <see cref="PromptTemplateConfig"/>.
    /// If not provided the service identifier will be the default value in <see cref="DefaultServiceId"/>.
    /// </remarks>
    [Experimental("SKEXP0001")]
    [JsonPropertyName("service_id")]
    public string? ServiceId
    {
        get => this._serviceId;

        set
        {
            this.ThrowIfFrozen();
            this._serviceId = value;
        }
    }

    /// <summary>
    /// Model identifier.
    /// This identifies the AI model these settings are configured for e.g., gpt-4, gpt-3.5-turbo
    /// </summary>
    [JsonPropertyName("model_id")]
    public string? ModelId
    {
        get => this._modelId;

        set
        {
            this.ThrowIfFrozen();
            this._modelId = value;
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
        get => this._extensionData;

        set
        {
            this.ThrowIfFrozen();
            this._extensionData = value;
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
        if (this.IsFrozen)
        {
            return;
        }

        this.IsFrozen = true;

        if (this._extensionData is not null)
        {
            this._extensionData = new ReadOnlyDictionary<string, object>(this._extensionData);
        }
    }

    /// <summary>
    /// Creates a new <see cref="PromptExecutionSettings"/> object that is a copy of the current instance.
    /// </summary>
    public virtual PromptExecutionSettings Clone() => new()
    {
        ModelId = this.ModelId,
        ServiceId = this.ServiceId,
        ExtensionData = this.ExtensionData is not null
            ? new Dictionary<string, object>(this.ExtensionData)
            : null
    };

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if the <see cref="PromptExecutionSettings"/> are frozen.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    protected void ThrowIfFrozen()
    {
        if (this.IsFrozen)
        {
            throw new InvalidOperationException("PromptExecutionSettings are frozen and cannot be modified.");
        }
    }

    #region private ================================================================================

    private string? _modelId;

    private IDictionary<string, object>? _extensionData;

    private string? _serviceId;

    #endregion

}
