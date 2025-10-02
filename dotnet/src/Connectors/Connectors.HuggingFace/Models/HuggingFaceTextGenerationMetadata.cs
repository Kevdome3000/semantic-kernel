// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.HuggingFace;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Core;


/// <summary>
/// Represents the metadata of a Hugging Face chat completion.
/// </summary>
public sealed class HuggingFaceTextGenerationMetadata : ReadOnlyDictionary<string, object?>
{

    internal HuggingFaceTextGenerationMetadata() : base(new Dictionary<string, object?>())
    {
    }


    internal HuggingFaceTextGenerationMetadata(TextGenerationResponse response) : this()
    {
        GeneratedTokens = response.FirstOrDefault()?.
            Details?.GeneratedTokens;

        FinishReason = response.FirstOrDefault()?.
            Details?.FinishReason;

        Tokens = response.FirstOrDefault()?.
            Details?.Tokens;

        PrefillTokens = response.FirstOrDefault()?.
            Details?.Prefill;
    }


    private HuggingFaceTextGenerationMetadata(IDictionary<string, object?> dictionary) : base(dictionary)
    {
    }


    /// <summary>
    /// The list of tokens used on the generation.
    /// </summary>
    public object? Tokens
    {
        get => GetValueFromDictionary(nameof(Tokens));
        internal init => SetValueInDictionary(value, nameof(Tokens));
    }

    /// <summary>
    /// The list of prefill tokens used on the generation.
    /// </summary>
    public object? PrefillTokens
    {
        get => GetValueFromDictionary(nameof(PrefillTokens));
        internal init => SetValueInDictionary(value, nameof(PrefillTokens));
    }

    /// <summary>
    /// Number of generated tokens.
    /// </summary>
    public int? GeneratedTokens
    {
        get => GetValueFromDictionary(nameof(GeneratedTokens)) as int?;
        internal init => SetValueInDictionary(value, nameof(GeneratedTokens));
    }

    /// <summary>
    /// Finish reason.
    /// </summary>
    public string? FinishReason
    {
        get => GetValueFromDictionary(nameof(FinishReason)) as string;
        internal init => SetValueInDictionary(value, nameof(FinishReason));
    }


    /// <summary>
    /// Converts a dictionary to a <see cref="HuggingFaceChatCompletionMetadata"/> object.
    /// </summary>
    public static HuggingFaceTextGenerationMetadata FromDictionary(IReadOnlyDictionary<string, object?> dictionary) => dictionary switch
    {
        null => throw new ArgumentNullException(nameof(dictionary)),
        HuggingFaceTextGenerationMetadata metadata => metadata,
        IDictionary<string, object?> metadata => new HuggingFaceTextGenerationMetadata(metadata),
        _ => new HuggingFaceTextGenerationMetadata(dictionary.ToDictionary(pair => pair.Key, pair => pair.Value))
    };


    private void SetValueInDictionary(object? value, string propertyName)
        => Dictionary[propertyName] = value;


    private object? GetValueFromDictionary(string propertyName)
        => Dictionary.TryGetValue(propertyName, out var value)
            ? value
            : null;

}
