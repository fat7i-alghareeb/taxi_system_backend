namespace Taxi.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public OutboxMessage(
        Guid id,
        DateTimeOffset occurredAtUtc,
        string type,
        string content)
    {
        Id = id;
        OccurredAtUtc = occurredAtUtc;
        Type = type;
        Content = content;
    }

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public int RetryCount { get; private set; }

    public string? Error { get; private set; }

    public void MarkProcessed(DateTimeOffset processedAtUtc)
    {
        ProcessedAtUtc = processedAtUtc;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        Error = error.Length <= 2000 ? error : error[..2000];
    }
}
