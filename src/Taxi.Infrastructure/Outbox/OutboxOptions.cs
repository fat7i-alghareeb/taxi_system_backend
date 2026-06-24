namespace Taxi.Infrastructure.Outbox;

/// <summary>
/// Tunable settings for the transactional-outbox dispatcher, bound from the
/// "Outbox" configuration section. Every value has a sensible default, so the
/// section is optional — override it in appsettings only when you need to.
/// Consumed by <see cref="OutboxDispatcherService"/> via <c>IOptions&lt;OutboxOptions&gt;</c>.
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>Configuration section name: <c>"Outbox"</c>.</summary>
    public const string SectionName = "Outbox";

    /// <summary>Delay between polls while messages are still being processed (drains a backlog quickly).</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Longer delay used after an empty poll, so an idle system does not hammer the database.</summary>
    public TimeSpan IdlePollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Maximum number of messages claimed and dispatched per poll.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>Dispatch attempts before a message is moved to the dead-letter state.</summary>
    public int MaxRetryCount { get; set; } = 10;

    /// <summary>How long a claimed message stays locked before another dispatcher may reclaim it (crash safety).</summary>
    public TimeSpan ClaimDuration { get; set; } = TimeSpan.FromMinutes(5);
}
