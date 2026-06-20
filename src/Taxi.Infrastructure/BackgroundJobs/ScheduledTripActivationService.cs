using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.BackgroundJobs;

/// <summary>
/// Produces admin-only preparation and escalation reminders for scheduled trips.
/// It never changes trip workflow status and never sends customer notifications.
/// </summary>
public sealed class ScheduledTripActivationService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<ScheduledTripActivationService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Scheduled trip reminder service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await EnqueueDueRemindersAsync(stoppingToken);
            await Task.Delay(CheckInterval, timeProvider, stoppingToken);
        }
    }

    private async Task EnqueueDueRemindersAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var now = timeProvider.GetUtcNow();

        var trips = await context.Trips
            .Where(t => t.ScheduledAtUtc != null &&
                (t.Status == TripStatus.AwaitingAdminAcceptance ||
                 t.Status == TripStatus.Accepted))
            .Where(t => t.ScheduledAtUtc <= now.AddHours(1))
            .ToListAsync(ct);

        var hasChanges = false;
        foreach (var trip in trips)
        {
            var stage = ResolveCurrentStage(trip, now);
            if (stage is null || trip.HasReminderBeenSent(stage.Value))
            {
                continue;
            }

            trip.MarkReminderSent(stage.Value, now);
            hasChanges = true;
            logger.LogInformation(
                "Queued scheduled trip reminder {Stage} for trip {TripId}.",
                stage,
                trip.Id);
        }

        if (hasChanges)
        {
            await context.SaveChangesAsync(ct);
        }
    }

    private static ScheduledTripReminderStage? ResolveCurrentStage(
        Trip trip,
        DateTimeOffset now)
    {
        var remaining = trip.ScheduledAtUtc!.Value - now;

        if (trip.Status == TripStatus.AwaitingAdminAcceptance)
        {
            if (remaining <= TimeSpan.Zero)
            {
                return ScheduledTripReminderStage.UnacceptedOverdue;
            }

            if (remaining <= TimeSpan.FromMinutes(15))
            {
                return ScheduledTripReminderStage.Unaccepted15Minutes;
            }

            if (remaining <= TimeSpan.FromMinutes(30))
            {
                return ScheduledTripReminderStage.Unaccepted30Minutes;
            }

            if (remaining <= TimeSpan.FromMinutes(60))
            {
                return ScheduledTripReminderStage.Unaccepted60Minutes;
            }

            return null;
        }

        if (remaining <= TimeSpan.Zero)
        {
            return null;
        }

        if (remaining <= TimeSpan.FromMinutes(15))
        {
            return ScheduledTripReminderStage.Accepted15Minutes;
        }

        if (remaining <= TimeSpan.FromMinutes(30))
        {
            return ScheduledTripReminderStage.Accepted30Minutes;
        }

        return null;
    }
}
