using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.BackgroundJobs;

public sealed class ScheduledTripActivationService(
    IServiceScopeFactory scopeFactory,
    ILogger<ScheduledTripActivationService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<ScheduledTripActivationService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduledTripActivationService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await ActivateDueTripsAsync(stoppingToken);
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task ActivateDueTripsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTimeOffset.UtcNow;

        var dueTrips = await context.Trips
            .Where(t => t.Status == TripStatus.Scheduled && t.ScheduledAtUtc <= now)
            .ToListAsync(ct);

        if (dueTrips.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Activating {Count} scheduled trip(s).", dueTrips.Count);

        foreach (var trip in dueTrips)
        {
            var result = trip.ActivateScheduled();
            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to activate scheduled trip {TripId}: {Error}",
                    trip.Id,
                    result.Error);
                continue;
            }

            try
            {
                // SaveChangesAsync dispatches domain events (TripRequested → admin FCM).
                await context.SaveChangesAsync(ct);

                await notificationService.SendPushNotificationAsync(
                    trip.PassengerId,
                    LocalizationKeys.Notification.TripScheduledActivatedTitle,
                    LocalizationKeys.Notification.TripScheduledActivatedBody,
                    new Dictionary<string, string>
                    {
                        { "tripId", trip.Id.ToString() },
                        { "status", "PendingDriver" },
                    },
                    ct);

                _logger.LogInformation("Activated scheduled trip {TripId}.", trip.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving/notifying for scheduled trip {TripId}.", trip.Id);
            }
        }
    }
}
