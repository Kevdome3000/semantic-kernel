// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Connectors.HuggingFace;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


/// <summary>
/// Represents the metadata of a Hugging Face chat completion.
/// </summary>
public sealed class HuggingFaceChatCompletionMetadata : ReadOnlyDictionary<string, object?>
{

    internal HuggingFaceChatCompletionMetadata() : base(new Dictionary<string, object?>())
    {
    }


    private HuggingFaceChatCompletionMetadata(IDictionary<string, object?> dictionary) : base(dictionary)
    {
    }


    /// <summary>
    /// Object identifier.
    /// </summary>
#pragma warning disable CA1720 // Identifier contains type name
    public string? Object
    {
        get => GetValueFromDictionary(nameof(Object)) as string;
        internal init => SetValueInDictionary(value, nameof(Object));
    }
#pragma warning restore CA1720 // Identifier contains type name

    /// <summary>
    /// Creation time of the response.
    /// </summary>
    public long? Created
    {
        get => (GetValueFromDictionary(nameof(Created)) as long?) ?? 0;
        internal init => SetValueInDictionary(value, nameof(Created));
    }

    /// <summary>
    /// Model used to generate the response.
    /// </summary>
    public string? Model
    {
        get => GetValueFromDictionary(nameof(Model)) as string;
        internal init => SetValueInDictionary(value, nameof(Model));
    }

    /// <summary>
    /// Reason why the processing was finished.
    /// </summary>
    public string? FinishReason
    {
        get => GetValueFromDictionary(nameof(FinishReason)) as string;
        internal init => SetValueInDictionary(value, nameof(FinishReason));
    }

    /// <summary>
    /// System fingerprint.
    /// </summary>
    public string? SystemFingerPrint
    {
        get => GetValueFromDictionary(nameof(SystemFingerPrint)) as string;
        internal init => SetValueInDictionary(value, nameof(SystemFingerPrint));
    }

    /// <summary>
    /// Id of the response.
    /// </summary>
    public string? Id
    {
        get => GetValueFromDictionary(nameof(Id)) as string;
        internal init => SetValueInDictionary(value, nameof(Id));
    }

    /// <summary>
    /// The total count of tokens used.
    /// </summary>
    /// <remarks>
    /// Usage is not available for streaming chunks.
    /// </remarks>
    public int? UsageTotalTokens
    {
        get => (GetValueFromDictionary(nameof(UsageTotalTokens)) as int?);
        internal init => SetValueInDictionary(value, nameof(UsageTotalTokens));
    }

    /// <summary>
    /// The count of tokens in the prompt.
    /// </summary>
    /// <remarks>
    /// Usage is not available for streaming chunks.
    /// </remarks>
    public int? UsagePromptTokens
    {
        get => (GetValueFromDictionary(nameof(UsagePromptTokens)) as int?);
        internal init => SetValueInDictionary(value, nameof(UsagePromptTokens));
    }

    /// <summary>
    /// The count of token in the current completion.
    /// </summary>
    /// <remarks>
    /// Usage is not available for streaming chunks.
    /// </remarks>
    public int? UsageCompletionTokens
    {
        get => (GetValueFromDictionary(nameof(UsageCompletionTokens)) as int?);
        internal init => SetValueInDictionary(value, nameof(UsageCompletionTokens));
    }

    /// <summary>
    /// The log probabilities of the completion.
    /// </summary>
    public object? LogProbs
    {
        get => GetValueFromDictionary(nameof(LogProbs));
        internal init => SetValueInDictionary(value, nameof(LogProbs));
    }


    /// <summary>
    /// Converts a dictionary to a <see cref="HuggingFaceChatCompletionMetadata"/> object.
    /// </summary>
    public static HuggingFaceChatCompletionMetadata FromDictionary(IReadOnlyDictionary<string, object?> dictionary) => dictionary switch
    {
        null => throw new ArgumentNullException(nameof(dictionary)),
        HuggingFaceChatCompletionMetadata metadata => metadata,
        IDictionary<string, object?> metadata => new HuggingFaceChatCompletionMetadata(metadata),
        _ => new HuggingFaceChatCompletionMetadata(dictionary.ToDictionary(pair => pair.Key, pair => pair.Value))
    };


    private void SetValueInDictionary(object? value, string propertyName)
        => Dictionary[propertyName] = value;


    private object? GetValueFromDictionary(string propertyName)
        => Dictionary.TryGetValue(propertyName, out var value)
            ? value
            : null;

}
