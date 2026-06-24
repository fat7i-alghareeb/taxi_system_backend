using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Taxi.Domain.Common;
using Taxi.Infrastructure.Data;

namespace Taxi.Infrastructure.Outbox;

/// <summary>
/// Background service that delivers the transactional outbox. On each poll it:
///   1. claims a batch of pending <see cref="OutboxMessage"/> rows (atomic and
///      concurrency-safe, so multiple instances never dispatch the same message),
///   2. deserializes each row back into its domain event and publishes it via MediatR,
///   3. marks the row processed, or failed with exponential-backoff retry and eventual
///      dead-lettering after <see cref="OutboxOptions.MaxRetryCount"/> attempts.
/// Rows are enqueued elsewhere by <see cref="ConvertDomainEventsToOutboxInterceptor"/>;
/// this service only drains them. It polls fast while there is work and backs off to the
/// idle interval when the table is empty, so an idle system barely touches the database.
/// All tuning lives in <see cref="OutboxOptions"/>.
/// </summary>
public sealed class OutboxDispatcherService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDispatcherService> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = 0;
            try
            {
                processed = await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox batch dispatch failed; polling will continue.");
            }

            // Drain a backlog quickly, but back off when there was nothing to do so we
            // don't hammer the database (and its query log) every second while idle.
            var delay = processed > 0 ? _options.PollInterval : _options.IdlePollInterval;
            await Task.Delay(delay, timeProvider, stoppingToken);
        }
    }

    private async Task<int> DispatchBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var now = timeProvider.GetUtcNow();
        var lockId = Guid.NewGuid();

        // 1. Pick a batch of claimable ids: not processed, not dead-lettered, due for a
        //    (re)attempt, and not currently locked by another dispatcher.
        var candidateIds = await context.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null
                && m.DeadLetteredAtUtc == null
                && (m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= now)
                && (m.LockedUntilUtc == null || m.LockedUntilUtc <= now))
            .OrderBy(m => m.OccurredAtUtc)
            .Select(m => m.Id)
            .Take(_options.BatchSize)
            .ToListAsync(ct);

        if (candidateIds.Count == 0)
        {
            return 0;
        }

        // 2. Claim them with a single atomic UPDATE. The WHERE clause re-checks the lock,
        //    so if two dispatchers race for the same rows each row is won by exactly one
        //    of them and the loser's UPDATE simply matches nothing — no double dispatch.
        //    This is the LINQ equivalent of the previous "FOR UPDATE SKIP LOCKED" claim,
        //    with no raw SQL and no manually managed transaction.
        Guid? claimLockId = lockId;
        DateTimeOffset? claimLockedUntil = now.Add(_options.ClaimDuration);
        await context.OutboxMessages
            .Where(m => candidateIds.Contains(m.Id)
                && (m.LockedUntilUtc == null || m.LockedUntilUtc <= now))
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(m => m.LockId, claimLockId)
                    .SetProperty(m => m.LockedUntilUtc, claimLockedUntil),
                ct);

        // 3. Load exactly the rows this instance won (tracked, so the dispatch loop can update them).
        var messages = await context.OutboxMessages
            .Where(m => m.LockId == lockId)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type, throwOnError: true)!;
                var domainEvent = JsonSerializer.Deserialize(message.Content, eventType) as DomainEvent
                    ?? throw new InvalidOperationException($"Could not deserialize outbox event '{message.Type}'.");

                await mediator.Publish(domainEvent, ct);
                message.MarkProcessed(timeProvider.GetUtcNow());
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.ToString(), timeProvider.GetUtcNow(), _options.MaxRetryCount);
                logger.LogError(ex, "Failed to dispatch outbox message {MessageId}.", message.Id);
                if (message.DeadLetteredAtUtc is not null)
                {
                    logger.LogCritical(
                        "Outbox message {MessageId} entered dead-letter state after {RetryCount} attempts.",
                        message.Id,
                        message.RetryCount);
                }
            }

            await context.SaveChangesAsync(ct);
        }

        if (messages.Count > 0)
        {
            logger.LogInformation(
                "Outbox dispatcher {LockId} processed claimed batch of {MessageCount} messages.",
                lockId,
                messages.Count);
        }

        return messages.Count;
    }
}
