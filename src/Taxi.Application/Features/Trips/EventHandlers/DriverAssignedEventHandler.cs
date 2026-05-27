using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class DriverAssignedEventHandler(
    ITripNotifier notifier,
    INotificationService notificationService)
    : INotificationHandler<DriverAssigned>
{
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;

    public async Task Handle(DriverAssigned notification, CancellationToken ct)
    {
        await _notifier.NotifyDriverAssignedAsync(
            notification.TripId,
            notification.PassengerId,
            notification.DriverId,
            ct);

        await _notificationService.SendPushNotificationAsync(
            notification.PassengerId,
            LocalizationKeys.Notification.DriverAssignedTitle,
            LocalizationKeys.Notification.DriverAssignedBody,
            new Dictionary<string, string>
            {
                { "tripId", notification.TripId.ToString() },
                { "status", "DriverAssigned" },
            },
            ct);
    }
}
