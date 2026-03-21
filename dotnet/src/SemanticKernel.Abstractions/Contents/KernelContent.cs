// Copyright (c) Microsoft.All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.Agents;

namespace Microsoft.SemanticKernel;

/// <summary>
/// Base class for all AI non-streaming results
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(TextContent), nameof(TextContent))]
[JsonDerivedType(typeof(ImageContent), nameof(ImageContent))]
[JsonDerivedType(typeof(FunctionCallContent), nameof(FunctionCallContent))]
[JsonDerivedType(typeof(FunctionResultContent), nameof(FunctionResultContent))]
[JsonDerivedType(typeof(BinaryContent), nameof(BinaryContent))]
[JsonDerivedType(typeof(AudioContent), nameof(AudioContent))]
[JsonDerivedType(typeof(ChatMessageContent), nameof(ChatMessageContent))]
#pragma warning disable SKEXP0110
[JsonDerivedType(typeof(ActionContent), nameof(ActionContent))]
[JsonDerivedType(typeof(ReasoningContent), nameof(ReasoningContent))]
[JsonDerivedType(typeof(AnnotationContent), nameof(AnnotationContent))]
[JsonDerivedType(typeof(FileReferenceContent), nameof(FileReferenceContent))]
#pragma warning disable SKEXP0110
public abstract class KernelContent
{
    /// <summary>
    /// MIME type of the content.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MimeType { get; set; }

    /// <summary>
    /// The inner content representation. Use this to bypass the current abstraction.
    /// </summary>
    /// <remarks>
    /// The usage of this property is considered "unsafe". Use it only if strictly necessary.
    /// </remarks>
    [JsonIgnore]
    public object? InnerContent { get; set; }

    /// <summary>
    /// The model ID used to generate the content.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ModelId { get; set; }

    /// <summary>
    /// The metadata associated with the content.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object?>? Metadata { get; set; }


    /// <summary>
    /// Initializes a new instance of the <see cref="KernelContent"/> class.
    /// </summary>
    protected KernelContent()
    {
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="KernelContent"/> class.
    /// </summary>
    /// <param name="innerContent">The inner content representation</param>
    /// <param name="modelId">The model ID used to generate the content</param>
    /// <param name="metadata">Metadata associated with the content</param>
    protected KernelContent(object? innerContent, string? modelId = null, IReadOnlyDictionary<string, object?>? metadata = null)
    {
        ModelId = modelId;
        InnerContent = innerContent;
        Metadata = metadata;
    }
}
