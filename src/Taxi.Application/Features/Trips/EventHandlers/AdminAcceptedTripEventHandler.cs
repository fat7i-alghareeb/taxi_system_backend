using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class AdminAcceptedTripEventHandler(
    ITripNotifier notifier,
    INotificationService notificationService)
    : INotificationHandler<AdminAcceptedTrip>
{
    public async Task Handle(AdminAcceptedTrip notification, CancellationToken ct)
    {
        await notifier.NotifyAdminAcceptedAsync(
            notification.TripId,
            notification.PassengerId,
            notification.AdminId,
            ct,
            notification.EventId);

        var scheduledLabel = notification.ScheduledAtUtc?.ToString("yyyy-MM-dd HH:mm 'UTC'")
            ?? "now";
        await notificationService.SendPushNotificationAsync(
            notification.PassengerId,
            LocalizationKeys.Notification.TripAcceptedTitle,
            LocalizationKeys.Notification.TripAcceptedBody,
            new Dictionary<string, string>
            {
                { "tripId", notification.TripId.ToString() },
                { "status", "Accepted" },
                { "type", "trip_accepted" },
                { "eventId", notification.EventId.ToString() },
            },
            ct,
            bodyArgs: [scheduledLabel]);
    }
}
