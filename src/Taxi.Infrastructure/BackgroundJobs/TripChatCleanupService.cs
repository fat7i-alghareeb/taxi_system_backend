using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.BackgroundJobs;

/// <summary>
/// Deletes in-trip chat messages (and their uploaded photos) a short grace window
/// after a trip ends, keeping storage small. The chat is closed for clients
/// immediately on completion/cancellation (see the trip event handlers); this job
/// only performs the deferred hard delete.
/// </summary>
public sealed class TripChatCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<TripChatCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    /// <summary>How long after a trip ends the chat stays readable before deletion.</summary>
    private static readonly TimeSpan GraceWindow = TimeSpan.FromHours(1);

    private static readonly TripStatus[] TerminalStatuses =
    [
        TripStatus.Completed,
        TripStatus.Cancelled,
        TripStatus.Refunded,
        TripStatus.PaymentFailed,
    ];

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<TripChatCleanupService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TripChatCleanupService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "TripChatCleanupService cleanup pass failed.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var threshold = DateTimeOffset.UtcNow - GraceWindow;

        // Only consider trips that actually have messages and ended before the threshold.
        // Cancelled/payment-failed trips have no CompletedAtUtc, so fall back to the audit
        // timestamp of the row's last change (which is when it became terminal).
        var staleTripIds = await (
            from m in context.TripMessages
            join t in context.Trips on m.TripId equals t.Id
            where TerminalStatuses.Contains(t.Status)
                && (t.CompletedAtUtc ?? t.LastModifiedUtc ?? t.CreatedAtUtc) <= threshold
            select m.TripId)
            .Distinct()
            .ToListAsync(ct);

        if (staleTripIds.Count == 0)
        {
            return;
        }

        foreach (var tripId in staleTripIds)
        {
            var messages = await context.TripMessages
                .Where(m => m.TripId == tripId)
                .ToListAsync(ct);

            foreach (var message in messages.Where(m => !string.IsNullOrWhiteSpace(m.PhotoUrl)))
            {
                try
                {
                    await fileStorage.DeleteAsync(message.PhotoUrl!, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete chat photo for trip {TripId}.", tripId);
                }
            }

            context.TripMessages.RemoveRange(messages);
            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Cleaned up {Count} chat message(s) for ended trip {TripId}.",
                messages.Count,
                tripId);
        }
    }
}
