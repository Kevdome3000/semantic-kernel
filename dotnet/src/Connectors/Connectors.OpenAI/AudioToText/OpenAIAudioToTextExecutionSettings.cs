// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Text;


/// <summary>
/// Execution settings for OpenAI audio-to-text request.
/// </summary>
public sealed class OpenAIAudioToTextExecutionSettings : PromptExecutionSettings
{

    /// <summary>
    /// Filename or identifier associated with audio data.
    /// Should be in format {filename}.{extension}
    /// </summary>
    [JsonPropertyName("filename")]
    public string Filename
    {
        get => _filename;

        set
        {
            ThrowIfFrozen();
            _filename = value;
        }
    }

    /// <summary>
    /// An optional language of the audio data as two-letter ISO-639-1 language code (e.g. 'en' or 'es').
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language
    {
        get => _language;

        set
        {
            ThrowIfFrozen();
            _language = value;
        }
    }

    /// <summary>
    /// An optional text to guide the model's style or continue a previous audio segment. The prompt should match the audio language.
    /// </summary>
    [JsonPropertyName("prompt")]
    public string? Prompt
    {
        get => _prompt;

        set
        {
            ThrowIfFrozen();
            _prompt = value;
        }
    }

    /// <summary>
    /// The format of the transcript output, in one of these options: json, text, srt, verbose_json, or vtt. Default is 'json'.
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
    /// The sampling temperature, between 0 and 1.
    /// Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.
    /// If set to 0, the model will use log probability to automatically increase the temperature until certain thresholds are hit.
    /// Default is 0.
    /// </summary>
    [JsonPropertyName("temperature")]
    public float Temperature
    {
        get => _temperature;

        set
        {
            ThrowIfFrozen();
            _temperature = value;
        }
    }


    /// <summary>
    /// Creates an instance of <see cref="OpenAIAudioToTextExecutionSettings"/> class with default filename - "file.mp3".
    /// </summary>
    public OpenAIAudioToTextExecutionSettings()
        : this(DefaultFilename)
    {
    }


    /// <summary>
    /// Creates an instance of <see cref="OpenAIAudioToTextExecutionSettings"/> class.
    /// </summary>
    /// <param name="filename">Filename or identifier associated with audio data. Should be in format {filename}.{extension}</param>
    public OpenAIAudioToTextExecutionSettings(string filename) => _filename = filename;


    /// <inheritdoc/>
    public override PromptExecutionSettings Clone() => new OpenAIAudioToTextExecutionSettings(Filename)
    {
        ModelId = ModelId,
        ExtensionData = ExtensionData is not null
            ? new Dictionary<string, object>(ExtensionData)
            : null,
        Temperature = Temperature,
        ResponseFormat = ResponseFormat,
        Language = Language,
        Prompt = Prompt
    };


    /// <summary>
    /// Converts <see cref="PromptExecutionSettings"/> to derived <see cref="OpenAIAudioToTextExecutionSettings"/> type.
    /// </summary>
    /// <param name="executionSettings">Instance of <see cref="PromptExecutionSettings"/>.</param>
    /// <returns>Instance of <see cref="OpenAIAudioToTextExecutionSettings"/>.</returns>
    public static OpenAIAudioToTextExecutionSettings? FromExecutionSettings(PromptExecutionSettings? executionSettings)
    {
        if (executionSettings is null)
        {
            return new OpenAIAudioToTextExecutionSettings();
        }

        if (executionSettings is OpenAIAudioToTextExecutionSettings settings)
        {
            return settings;
        }

        var json = JsonSerializer.Serialize(executionSettings);

        var openAIExecutionSettings = JsonSerializer.Deserialize<OpenAIAudioToTextExecutionSettings>(json, JsonOptionsCache.ReadPermissive);

        if (openAIExecutionSettings is not null)
        {
            return openAIExecutionSettings;
        }

        throw new ArgumentException($"Invalid execution settings, cannot convert to {nameof(OpenAIAudioToTextExecutionSettings)}", nameof(executionSettings));
    }


    #region private ================================================================================

    private const string DefaultFilename = "file.mp3";

    private float _temperature;

    private string _responseFormat = "json";

    private string _filename;

    private string? _language;

    private string? _prompt;

    #endregion


}
