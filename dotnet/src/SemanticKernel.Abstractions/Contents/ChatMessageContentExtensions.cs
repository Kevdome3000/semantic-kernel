// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Microsoft.SemanticKernel;

/// <summary>Provides extension methods for <see cref="ChatMessageContent"/>.</summary>
[Experimental("SKEXP0001")]
public static class ChatMessageContentExtensions
{
    /// <summary>Converts a <see cref="ChatMessageContent"/> to a <see cref="ChatMessage"/>.</summary>
    /// <remarks>This conversion should not be necessary once SK eventually adopts the shared content types.</remarks>
    public static ChatMessage ToChatMessage(this ChatMessageContent content)
    {
        Verify.NotNull(content);

        ChatMessage message = new()
        {
            AdditionalProperties = content.Metadata is not null ? new(content.Metadata) : null,
            AuthorName = content.AuthorName,
            RawRepresentation = content.InnerContent,
            Role = content.Role.Label is string label ? new ChatRole(label) : ChatRole.User,
        };

        foreach (var item in content.Items)
        {
            AIContent? aiContent = null;
            switch (item)
            {
                case TextContent tc:
                    aiContent = new Microsoft.Extensions.AI.TextContent(tc.Text);
                    break;

                case ImageContent ic:
                    aiContent =
                        ic.DataUri is not null ? new DataContent(ic.DataUri, ic.MimeType) :
                        ic.Uri is not null ? new UriContent(ic.Uri, ic.MimeType ?? "image/*") :
                        null;
                    break;

                case AudioContent ac:
                    aiContent =
                        ac.DataUri is not null ? new DataContent(ac.DataUri, ac.MimeType) :
                        ac.Uri is not null ? new UriContent(ac.Uri, ac.MimeType ?? "audio/*") :
                        null;
                    break;

                case BinaryContent bc:
                    aiContent =
                        bc.DataUri is not null ? new DataContent(bc.DataUri, bc.MimeType) :
                        bc.Uri is not null ? new UriContent(bc.Uri, bc.MimeType ?? "application/octet-stream") :
                        null;
                    break;

                case FunctionCallContent fcc:
                    aiContent = new Microsoft.Extensions.AI.FunctionCallContent(fcc.Id ?? string.Empty, fcc.FunctionName, fcc.Arguments);
                    break;

                case FunctionResultContent frc:
                    aiContent = new Microsoft.Extensions.AI.FunctionResultContent(frc.CallId ?? string.Empty, frc.Result);
                    break;
            }

            if (aiContent is not null)
            {
                aiContent.RawRepresentation = item.InnerContent;
                aiContent.AdditionalProperties = item.Metadata is not null ? new(item.Metadata) : null;

                message.Contents.Add(aiContent);
            }
        }

        return message;
    }
}
