namespace Taxi.Domain.Notifications;

public enum NotificationType
{
    Sms,
    Push,
    Email
}

public class Notification
{
    public Notification(
        Guid userId,
        NotificationType type,
        string title,
        string body)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Type = type;
        Title = title;
        Body = body;
        CreatedAtUtc = DateTime.UtcNow;
        IsSent = false;
    }

    private Notification() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }
    public bool IsSent { get; private set; }
    public DateTime? SentAtUtc { get; private set; }

    public void MarkAsSent()
    {
        IsSent = true;
        SentAtUtc = DateTime.UtcNow;
    }
}

