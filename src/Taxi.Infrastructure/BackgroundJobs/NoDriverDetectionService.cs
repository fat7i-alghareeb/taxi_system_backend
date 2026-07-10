using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.BackgroundJobs;

/// <summary>
/// Detects trips that have sat in <see cref="TripStatus.AwaitingAdminAcceptance"/>
/// past their no-driver deadline and raises the customer-facing
/// "no driver found — postpone or cancel" prompt. Applies to immediate AND
/// scheduled trips (armed at payment confirmation).
/// </summary>
public sealed class NoDriverDetectionService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<NoDriverDetectionService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("No-driver detection service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RaiseDuePromptsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "No-driver detection pass failed; retrying next tick.");
            }

            await Task.Delay(CheckInterval, timeProvider, stoppingToken);
        }
    }

    private async Task RaiseDuePromptsAsync(CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();

        List<Guid> dueTripIds;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            dueTripIds = await context.Trips
                .Where(t => t.Status == TripStatus.AwaitingAdminAcceptance &&
                            !t.NoDriverDecisionRequired &&
                            t.NoDriverPromptDueAtUtc != null &&
                            t.NoDriverPromptDueAtUtc <= now)
                .Select(t => t.Id)
                .ToListAsync(ct);
        }

        // One scope per trip so a concurrency conflict on one row (e.g. an admin
        // accepting the trip between our read and save) never aborts the batch or
        // leaks a stale tracked entity / domain event into another trip's save.
        foreach (var tripId in dueTripIds)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == tripId, ct);
            if (trip is null)
            {
                continue;
            }

            var result = trip.RaiseNoDriverPrompt();
            if (result.IsFailure)
            {
                // State changed since the candidate query (accepted/cancelled) —
                // nothing to prompt.
                continue;
            }

            try
            {
                await context.SaveChangesAsync(ct);
                logger.LogInformation("Raised no-driver prompt for trip {TripId}.", tripId);
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogInformation(
                    "Skipped no-driver prompt for trip {TripId}; it was modified concurrently (likely accepted).",
                    tripId);
            }
        }
    }
}
