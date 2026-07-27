using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.BackgroundJobs;

/// <summary>
/// Produces reminders for scheduled trips: admin preparation/escalation reminders,
/// plus the 30- and 15-minute countdown reminders promised to the passenger at
/// booking time. It never changes trip workflow status.
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
            var dueStages = ScheduledTripReminderSchedule.ResolveDueStages(
                trip.Status,
                trip.ScheduledAtUtc!.Value,
                trip.CreatedAtUtc,
                now);

            foreach (var stage in dueStages)
            {
                if (trip.HasReminderBeenSent(stage))
                {
                    continue;
                }

                trip.MarkReminderSent(stage, now);
                hasChanges = true;
                logger.LogInformation(
                    "Queued scheduled trip reminder {Stage} for trip {TripId}.",
                    stage,
                    trip.Id);
            }
        }

        if (hasChanges)
        {
            await context.SaveChangesAsync(ct);
        }
    }
}
