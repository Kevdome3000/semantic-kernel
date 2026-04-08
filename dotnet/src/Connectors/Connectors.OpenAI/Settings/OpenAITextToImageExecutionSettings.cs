// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.Text;
using OpenAI.Images;

namespace Microsoft.SemanticKernel.Connectors.OpenAI;

/// <summary>
/// Text to image execution settings for an OpenAI image generation request.
/// </summary>
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public sealed class OpenAITextToImageExecutionSettings : PromptExecutionSettings
{
    /// <summary>
    /// Optional width and height of the generated image.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Must be one of <c>1024x1024, 1536x1024, 1024x1536, auto</c> for <c>gpt-image-1</c> model.</item>
    /// </list>
    /// </remarks>
    public (int Width, int Height)? Size
    {
        get => _size;

        set
        {
            ThrowIfFrozen();
            _size = value;
        }
    }

    /// <summary>
    /// The quality of the image that will be generated.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>standard</c>: creates images with standard quality. This is the default.</item>
    /// <item><c>hd</c> OR <c>high</c>: creates images with finer details and greater consistency.</item>
    /// <item><c>medium</c>: creates images with medium quality (supported by <c>gpt-image-1</c>).</item>
    /// <item><c>low</c>: creates images with lower quality for faster generation (supported by <c>gpt-image-1</c>).</item>
    /// </list>
    /// </remarks>
    [JsonPropertyName("quality")]
    public string? Quality
    {
        get => _quality;

        set
        {
            ThrowIfFrozen();
            _quality = value;
        }
    }

    /// <summary>
    /// The style of the generated images.
    /// </summary>
    /// <remarks>
    /// Must be one of <c>vivid</c> or <c>natural</c>.
    /// <list type="bullet">
    /// <item><c>vivid</c>: causes the model to lean towards generating hyper-real and dramatic images.</item>
    /// <item><c>natural</c>: causes the model to produce more natural, less hyper-real looking images.</item>
    /// </list>
    /// This param is not supported for <c>gpt-image-1</c> model.
    /// </remarks>
    [JsonPropertyName("style")]
    public string? Style
    {
        get => _style;

        set
        {
            ThrowIfFrozen();
            _style = value;
        }
    }

    /// <summary>
    /// The format of the generated images.
    /// Can be a <see cref="GeneratedImageFormat"/> or a <c>string</c> where:
    /// <list type="bullet">
    /// <item><see cref="GeneratedImageFormat"/>: causes the model to generated in the provided format</item>
    /// <item><c>url</c> OR <c>uri</c>: causes the model to return an url for the generated images.</item>
    /// <item><c>b64_json</c> or <c>bytes</c>: causes the model to return in a Base64 format the content of the images.</item>
    /// </list>
    /// </summary>
    [JsonPropertyName("response_format")]
    public object? ResponseFormat
    {
        get => _responseFormat;
        set
        {
            ThrowIfFrozen();
            _responseFormat = value;
        }
    }

    /// <summary>
    /// A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.
    /// </summary>
    [JsonPropertyName("user")]
    public string? EndUserId
    {
        get => _endUserId;
        set
        {
            ThrowIfFrozen();
            _endUserId = value;
        }
    }


    /// <inheritdoc/>
    public override void Freeze()
    {
        if (IsFrozen)
        {
            return;
        }

        base.Freeze();
    }


    /// <inheritdoc/>
    public override PromptExecutionSettings Clone()
    {
        return new OpenAITextToImageExecutionSettings
        {
            ModelId = ModelId,
            ExtensionData = ExtensionData is not null
                ? new Dictionary<string, object>(ExtensionData)
                : null,
            Size = Size
        };
    }


    /// <summary>
    /// Create a new settings object with the values from another settings object.
    /// </summary>
    /// <param name="executionSettings">Template configuration</param>
    /// <returns>An instance of OpenAIPromptExecutionSettings</returns>
    public static OpenAITextToImageExecutionSettings FromExecutionSettings(PromptExecutionSettings? executionSettings)
    {
        if (executionSettings is null)
        {
            return new OpenAITextToImageExecutionSettings();
        }

        if (executionSettings is OpenAITextToImageExecutionSettings settings)
        {
            return settings;
        }

        var json = JsonSerializer.Serialize(executionSettings);
        var openAIExecutionSettings = JsonSerializer.Deserialize<OpenAITextToImageExecutionSettings>(json, JsonOptionsCache.ReadPermissive)!;

        if (openAIExecutionSettings.ExtensionData?.TryGetValue("width", out var width) ?? false)
        {
            openAIExecutionSettings.Width = ((JsonElement)width).GetInt32();
        }

        if (openAIExecutionSettings.ExtensionData?.TryGetValue("height", out var height) ?? false)
        {
            openAIExecutionSettings.Height = ((JsonElement)height).GetInt32();
        }

        return openAIExecutionSettings!;
    }


    #region private ================================================================================

    [JsonPropertyName("width")]
    internal int? Width
    {
        get => Size?.Width;
        set
        {
            if (!value.HasValue) { return; }
            Size = (value.Value, Size?.Height ?? 0);
        }
    }

    [JsonPropertyName("height")]
    internal int? Height
    {
        get => Size?.Height;
        set
        {
            if (!value.HasValue) { return; }
            Size = (Size?.Width ?? 0, value.Value);
        }
    }

    private (int Width, int Height)? _size;
    private string? _quality;
    private string? _style;
    private object? _responseFormat;
    private string? _endUserId;

    #endregion


}
