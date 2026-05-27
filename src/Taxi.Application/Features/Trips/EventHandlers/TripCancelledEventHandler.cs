using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripCancelledEventHandler(
    ITripNotifier notifier,
    INotificationService notificationService)
    : INotificationHandler<TripCancelled>
{
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;

    public async Task Handle(TripCancelled notification, CancellationToken ct)
    {
        await _notifier.NotifyTripCancelledAsync(
            notification.TripId,
            notification.PassengerId,
            ct);

        await _notificationService.SendPushNotificationAsync(
            notification.PassengerId,
            LocalizationKeys.Notification.TripCancelledTitle,
            LocalizationKeys.Notification.TripCancelledBody,
            new Dictionary<string, string>
            {
                { "tripId", notification.TripId.ToString() },
                { "status", "Cancelled" },
            },
            ct);
    }
}
