// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel;

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using ChatCompletion;


/// <summary>
/// Represents chat message content return from a <see cref="IChatCompletionService" /> service.
/// </summary>
public class ChatMessageContent : KernelContent
{

    /// <summary>
    /// Role of the author of the message
    /// </summary>
    public AuthorRole Role { get; set; }

    /// <summary>
    /// A convenience property to get or set the text of the first item in the <see cref="Items" /> collection of <see cref="TextContent"/> type.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string? Content
    {
        get
        {
            var textContent = Items.OfType<TextContent>().
                FirstOrDefault();

            return textContent?.Text;
        }
        set
        {
            if (value == null)
            {
                return;
            }

            var textContent = Items.OfType<TextContent>().
                FirstOrDefault();

            if (textContent is not null)
            {
                textContent.Text = value;
                textContent.Encoding = Encoding;
            }
            else
            {
                Items.Add(new TextContent(
                        value,
                        ModelId,
                        InnerContent,
                        Encoding,
                        Metadata
                    )
                    { MimeType = MimeType });
            }
        }
    }

    /// <summary>
    /// Chat message content items
    /// </summary>
    public ChatMessageContentItemCollection Items
    {
        get => _items ??= new ChatMessageContentItemCollection();
        set => _items = value;
    }

    /// <summary>
    /// The encoding of the text content.
    /// </summary>
    [JsonIgnore]
    public Encoding Encoding
    {
        get
        {
            var textContent = Items.OfType<TextContent>().
                FirstOrDefault();

            if (textContent is not null)
            {
                return textContent.Encoding;
            }

            return _encoding;
        }
        set
        {
            _encoding = value;

            var textContent = Items.OfType<TextContent>().
                FirstOrDefault();

            if (textContent is not null)
            {
                textContent.Encoding = value;
            }
        }
    }

    /// <summary>
    /// Represents the source of the message.
    /// </summary>
    /// <remarks>
    /// The source is corresponds to the entity that generated this message.
    /// The property is intended to be used by agents to associate themselves with the messages they generate.
    /// </remarks>

    [JsonIgnore]
    public object? Source { get; set; }


    /// <summary>
    /// Creates a new instance of the <see cref="ChatMessageContent"/> class
    /// </summary>
    [JsonConstructor]
    public ChatMessageContent() => _encoding = Encoding.UTF8;


    /// <summary>
    /// Creates a new instance of the <see cref="ChatMessageContent"/> class
    /// </summary>
    /// <param name="role">Role of the author of the message</param>
    /// <param name="content">Content of the message</param>
    /// <param name="modelId">The model ID used to generate the content</param>
    /// <param name="innerContent">Inner content object reference</param>
    /// <param name="encoding">Encoding of the text</param>
    /// <param name="metadata">Dictionary for any additional metadata</param>
    public ChatMessageContent(
        AuthorRole role,
        string? content,
        string? modelId = null,
        object? innerContent = null,
        Encoding? encoding = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
        : base(innerContent, modelId, metadata)
    {
        Role = role;
        _encoding = encoding ?? Encoding.UTF8;
        Content = content;
    }


    /// <summary>
    /// Creates a new instance of the <see cref="ChatMessageContent"/> class
    /// </summary>
    /// <param name="role">Role of the author of the message</param>
    /// <param name="items">Instance of <see cref="ChatMessageContentItemCollection"/> with content items</param>
    /// <param name="modelId">The model ID used to generate the content</param>
    /// <param name="innerContent">Inner content object reference</param>
    /// <param name="encoding">Encoding of the text</param>
    /// <param name="metadata">Dictionary for any additional metadata</param>
    public ChatMessageContent(
        AuthorRole role,
        ChatMessageContentItemCollection items,
        string? modelId = null,
        object? innerContent = null,
        Encoding? encoding = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
        : base(innerContent, modelId, metadata)
    {
        Role = role;
        _encoding = encoding ?? Encoding.UTF8;
        _items = items;
    }


    /// <inheritdoc/>
    public override string ToString() => Content ?? string.Empty;


    private ChatMessageContentItemCollection? _items;

    private Encoding _encoding;

}
