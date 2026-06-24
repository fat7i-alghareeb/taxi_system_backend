namespace Taxi.Infrastructure.Outbox;

/// <summary>
/// A single row of the transactional outbox: a domain event captured (serialized to
/// <see cref="Content"/>) in the same transaction as the change that raised it, to be
/// dispatched later by <see cref="OutboxDispatcherService"/>. The state machine lives in
/// the <c>Mark*</c> methods and is driven entirely by the dispatcher:
/// <list type="bullet">
///   <item><see cref="ProcessedAtUtc"/> — set once the event was published successfully.</item>
///   <item><see cref="RetryCount"/> / <see cref="NextAttemptAtUtc"/> / <see cref="Error"/> —
///         track failed attempts and the exponential-backoff schedule.</item>
///   <item><see cref="LockId"/> / <see cref="LockedUntilUtc"/> — a time-boxed claim so a
///         crashed dispatcher's in-flight messages are eventually retried by another.</item>
///   <item><see cref="DeadLetteredAtUtc"/> — set after the retry budget is exhausted; the
///         message is then ignored until inspected manually.</item>
/// </list>
/// </summary>
public sealed class OutboxMessage
{
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

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public Guid? LockId { get; private set; }

    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public DateTimeOffset? DeadLetteredAtUtc { get; private set; }

    public void MarkClaimed(Guid lockId, DateTimeOffset lockedUntilUtc)
    {
        LockId = lockId;
        LockedUntilUtc = lockedUntilUtc;
    }

    public void MarkProcessed(DateTimeOffset processedAtUtc)
    {
        ProcessedAtUtc = processedAtUtc;
        NextAttemptAtUtc = null;
        LockId = null;
        LockedUntilUtc = null;
        Error = null;
    }

    public void MarkFailed(string error, DateTimeOffset failedAtUtc, int maxRetryCount)
    {
        RetryCount++;
        Error = error.Length <= 2000 ? error : error[..2000];
        LockId = null;
        LockedUntilUtc = null;

        if (RetryCount >= maxRetryCount)
        {
            DeadLetteredAtUtc = failedAtUtc;
            NextAttemptAtUtc = null;
            return;
        }

        var delaySeconds = Math.Min(900, 5 * Math.Pow(2, RetryCount - 1));
        NextAttemptAtUtc = failedAtUtc.AddSeconds(delaySeconds);
    }

    private OutboxMessage()
    {
    }
}
