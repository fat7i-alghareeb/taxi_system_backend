using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class DriverEnRouteEventHandler(ITripNotifier notifier, INotificationService notificationService)
    : INotificationHandler<DriverEnRoute>
{
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;

    public async Task Handle(DriverEnRoute notification, CancellationToken ct)
    {
        await _notifier.NotifyDriverEnRouteAsync(
            notification.TripId,
            notification.PassengerId,
            notification.DriverId,
            ct);

        await _notificationService.SendPushNotificationAsync(
            notification.PassengerId,
            "Notification.DriverEnRoute.Title",
            "Notification.DriverEnRoute.Body",
            new Dictionary<string, string>
            {
                { "tripId", notification.TripId.ToString() },
                { "status", "DriverEnRoute" }
            },
            ct);
    }
}
