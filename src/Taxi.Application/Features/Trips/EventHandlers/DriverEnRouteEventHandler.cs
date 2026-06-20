using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
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
            ct,
            notification.EventId);

        await _notificationService.SendPushNotificationAsync(
            notification.PassengerId,
            LocalizationKeys.Notification.DriverEnRouteTitle,
            LocalizationKeys.Notification.DriverEnRouteBody,
            new Dictionary<string, string>
            {
                { "tripId", notification.TripId.ToString() },
                { "status", "EnRoute" },
                { "type", "trip_en_route" },
                { "eventId", notification.EventId.ToString() },
            },
            ct);
    }
}
