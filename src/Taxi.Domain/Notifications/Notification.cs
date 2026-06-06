using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Notifications;

public enum NotificationType
{
    Sms,
    Push,
    Email
}

public sealed class Notification : Entity
{
    private Notification() { } // EF Core

    private Notification(
        Guid id,
        Guid userId,
        NotificationType type,
        string title,
        string body)
        : base(id)
    {
        UserId = userId;
        Type = type;
        Title = title;
        Body = body;
        CreatedAtUtc = DateTime.UtcNow;
        IsSent = false;
    }

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }
    public bool IsSent { get; private set; }
    public DateTime? SentAtUtc { get; private set; }

    public static Result<Notification> Create(
        Guid userId,
        NotificationType type,
        string title,
        string body)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return NotificationErrors.TitleRequired;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return NotificationErrors.BodyRequired;
        }

        return new Notification(Guid.NewGuid(), userId, type, title, body);
    }

    public Result<Success> MarkAsSent()
    {
        if (IsSent)
        {
            return Result.Success;
        }

        IsSent = true;
        SentAtUtc = DateTime.UtcNow;
        return Result.Success;
    }
}
