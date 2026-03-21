// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.Plugins.MsGraph.Models;

namespace Microsoft.SemanticKernel.Plugins.MsGraph.Connectors;

/// <summary>
/// Extensions for converting between Microsoft Graph models and plugin models.
/// </summary>
internal static class MicrosoftGraphModelExtensions
{
    /// <summary>
    /// Convert a Microsoft Graph message to an email message.
    /// </summary>
    public static EmailMessage ToEmailMessage(this Graph.Models.Message graphMessage)
        => new()
        {
            BccRecipients = graphMessage.BccRecipients?.Select(r => r.EmailAddress!.ToEmailAddress()),
            Body = graphMessage.Body?.Content,
#pragma warning disable CA1307 // Specify StringComparison for clarity
            BodyPreview = graphMessage.BodyPreview?.Replace("\u200C", ""), // BodyPreviews are sometimes filled with zero-width non-joiner characters - remove them.
#pragma warning restore CA1307
            CcRecipients = graphMessage.CcRecipients?.Select(r => r.EmailAddress!.ToEmailAddress()),
            From = graphMessage.From?.EmailAddress?.ToEmailAddress(),
            IsRead = graphMessage.IsRead,
            ReceivedDateTime = graphMessage.ReceivedDateTime,
            Recipients = graphMessage.ToRecipients?.Select(r => r.EmailAddress!.ToEmailAddress()),
            SentDateTime = graphMessage.SentDateTime,
            Subject = graphMessage.Subject
        };


    /// <summary>
    /// Convert a Microsoft Graph email address to an email address.
    /// </summary>
    public static EmailAddress ToEmailAddress(this Microsoft.Graph.Models.EmailAddress graphEmailAddress)
    {
        return new EmailAddress
        {
            Address = graphEmailAddress.Address,
            Name = graphEmailAddress.Name
        };
    }


    /// <summary>
    /// Convert a calendar event to a Microsoft Graph event.
    /// </summary>
    public static Graph.Models.Event ToGraphEvent(this CalendarEvent calendarEvent)
    {
        return new()
        {
            Subject = calendarEvent.Subject,
            Body = new Graph.Models.ItemBody { Content = calendarEvent.Content, ContentType = Microsoft.Graph.Models.BodyType.Html },
            Start = calendarEvent.Start.HasValue
                ? calendarEvent.Start.Value.ToDateTimeTimeZone()
                : System.DateTime.Now.ToDateTimeTimeZone(),
            End = calendarEvent.End.HasValue
                ? calendarEvent.End.Value.ToDateTimeTimeZone()
                : (System.DateTime.Now + TimeSpan.FromHours(1)).ToDateTimeTimeZone(),
            Location = new Microsoft.Graph.Models.Location { DisplayName = calendarEvent.Location },
            Attendees = calendarEvent.Attendees?.Select(a => new Microsoft.Graph.Models.Attendee { EmailAddress = new Microsoft.Graph.Models.EmailAddress { Address = a } })?.ToList()
        };
    }


    /// <summary>
    /// Convert a Microsoft Graph event to a calendar event.
    /// </summary>
    public static CalendarEvent ToCalendarEvent(this Graph.Models.Event msGraphEvent)
    {
        return new CalendarEvent
        {
            Subject = msGraphEvent.Subject,
            Content = msGraphEvent.Body?.Content,
            Start = msGraphEvent.Start?.ToDateTimeOffset(),
            End = msGraphEvent.End?.ToDateTimeOffset(),
            Location = msGraphEvent.Location?.DisplayName,
            Attendees = msGraphEvent.Attendees?.Where(a => a.EmailAddress?.Address is not null).Select(a => a.EmailAddress!.Address!)
        };
    }
}
