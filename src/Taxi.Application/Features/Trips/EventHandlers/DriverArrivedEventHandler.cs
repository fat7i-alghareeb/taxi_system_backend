using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class DriverArrivedEventHandler(ITripNotifier notifier, INotificationService notificationService)
    : INotificationHandler<DriverArrived>
{
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;

    public async Task Handle(DriverArrived notification, CancellationToken ct)
    {
        await _notifier.NotifyDriverArrivedAsync(
            notification.TripId,
            notification.PassengerId,
            notification.DriverId,
            ct,
            notification.EventId);

        await _notificationService.SendPushNotificationAsync(
            notification.PassengerId,
            LocalizationKeys.Notification.DriverArrivedTitle,
            LocalizationKeys.Notification.DriverArrivedBody,
            new Dictionary<string, string>
            {
                { "tripId", notification.TripId.ToString() },
                { "status", "Arrived" },
                { "type", "trip_arrived" },
                { "eventId", notification.EventId.ToString() },
                { "sound", "default" },
            },
            ct);
    }
}
