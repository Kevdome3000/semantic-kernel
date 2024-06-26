﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Text;


/// <summary>
/// Execution settings for OpenAI text-to-audio request.
/// </summary>
public sealed class OpenAITextToAudioExecutionSettings : PromptExecutionSettings
{

    /// <summary>
    /// The voice to use when generating the audio. Supported voices are alloy, echo, fable, onyx, nova, and shimmer.
    /// </summary>
    [JsonPropertyName("voice")]
    public string Voice
    {
        get => _voice;

        set
        {
            ThrowIfFrozen();
            _voice = value;
        }
    }

    /// <summary>
    /// The format to audio in. Supported formats are mp3, opus, aac, and flac.
    /// </summary>
    [JsonPropertyName("response_format")]
    public string ResponseFormat
    {
        get => _responseFormat;

        set
        {
            ThrowIfFrozen();
            _responseFormat = value;
        }
    }

    /// <summary>
    /// The speed of the generated audio. Select a value from 0.25 to 4.0. 1.0 is the default.
    /// </summary>
    [JsonPropertyName("speed")]
    public float Speed
    {
        get => _speed;

        set
        {
            ThrowIfFrozen();
            _speed = value;
        }
    }


    /// <summary>
    /// Creates an instance of <see cref="OpenAITextToAudioExecutionSettings"/> class with default voice - "alloy".
    /// </summary>
    public OpenAITextToAudioExecutionSettings()
        : this(DefaultVoice)
    {
    }


    /// <summary>
    /// Creates an instance of <see cref="OpenAITextToAudioExecutionSettings"/> class.
    /// </summary>
    /// <param name="voice">The voice to use when generating the audio. Supported voices are alloy, echo, fable, onyx, nova, and shimmer.</param>
    public OpenAITextToAudioExecutionSettings(string voice) => _voice = voice;


    /// <inheritdoc/>
    public override PromptExecutionSettings Clone() => new OpenAITextToAudioExecutionSettings(Voice)
    {
        ModelId = ModelId,
        ExtensionData = ExtensionData is not null
            ? new Dictionary<string, object>(ExtensionData)
            : null,
        Speed = Speed,
        ResponseFormat = ResponseFormat
    };


    /// <summary>
    /// Converts <see cref="PromptExecutionSettings"/> to derived <see cref="OpenAITextToAudioExecutionSettings"/> type.
    /// </summary>
    /// <param name="executionSettings">Instance of <see cref="PromptExecutionSettings"/>.</param>
    /// <returns>Instance of <see cref="OpenAITextToAudioExecutionSettings"/>.</returns>
    public static OpenAITextToAudioExecutionSettings? FromExecutionSettings(PromptExecutionSettings? executionSettings)
    {
        if (executionSettings is null)
        {
            return new OpenAITextToAudioExecutionSettings();
        }

        if (executionSettings is OpenAITextToAudioExecutionSettings settings)
        {
            return settings;
        }

        var json = JsonSerializer.Serialize(executionSettings);

        var openAIExecutionSettings = JsonSerializer.Deserialize<OpenAITextToAudioExecutionSettings>(json, JsonOptionsCache.ReadPermissive);

        if (openAIExecutionSettings is not null)
        {
            return openAIExecutionSettings;
        }

        throw new ArgumentException($"Invalid execution settings, cannot convert to {nameof(OpenAITextToAudioExecutionSettings)}", nameof(executionSettings));
    }


    #region private ================================================================================

    private const string DefaultVoice = "alloy";

    private float _speed = 1.0f;

    private string _responseFormat = "mp3";

    private string _voice;

    #endregion


}
