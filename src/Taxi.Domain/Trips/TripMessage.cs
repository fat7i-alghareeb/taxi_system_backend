using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Trips;

/// <summary>
/// Who sent an in-trip chat message. A user acting as an admin in the staff app
/// is recorded as <see cref="Admin"/> even if they are also the assigned driver.
/// </summary>
public enum TripMessageSenderRole
{
    Passenger,
    Driver,
    Admin
}

/// <summary>
/// A single message in the temporary, per-trip chat between the passenger, the
/// assigned driver, and (optionally) an admin. Messages are deleted by a
/// background cleanup job a short while after the trip ends — they are NOT part
/// of the <see cref="Trip"/> aggregate and carry no trip invariants.
/// </summary>
public sealed class TripMessage : AuditableEntity
{
    public const int MaxContentLength = 2000;

    private TripMessage() { } // EF Core

    private TripMessage(
        Guid id,
        Guid tripId,
        Guid senderId,
        TripMessageSenderRole senderRole,
        string? content,
        string? photoUrl)
        : base(id)
    {
        TripId = tripId;
        SenderId = senderId;
        SenderRole = senderRole;
        Content = content;
        PhotoUrl = photoUrl;
        SentAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid TripId { get; private set; }
    public Guid SenderId { get; private set; }
    public TripMessageSenderRole SenderRole { get; private set; }

    /// <summary>Text body. Null when the message is a photo with no caption.</summary>
    public string? Content { get; private set; }

    /// <summary>URL of an attached photo, or null for a text-only message.</summary>
    public string? PhotoUrl { get; private set; }

    public DateTimeOffset SentAtUtc { get; private set; }

    public static Result<TripMessage> Create(
        Guid id,
        Guid tripId,
        Guid senderId,
        TripMessageSenderRole senderRole,
        string? content,
        string? photoUrl)
    {
        var normalizedContent = string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        var normalizedPhoto = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl;

        if (normalizedContent is null && normalizedPhoto is null)
        {
            return TripErrors.EmptyMessage;
        }

        if (normalizedContent is { Length: > MaxContentLength })
        {
            return TripErrors.MessageTooLong;
        }

        return new TripMessage(id, tripId, senderId, senderRole, normalizedContent, normalizedPhoto);
    }
}
