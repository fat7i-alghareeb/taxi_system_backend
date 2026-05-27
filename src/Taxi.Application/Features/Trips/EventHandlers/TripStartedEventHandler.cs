using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripStartedEventHandler(
    ITripNotifier notifier,
    INotificationService notificationService)
    : INotificationHandler<TripStarted>
{
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;

    public async Task Handle(TripStarted notification, CancellationToken ct)
    {
        await _notifier.NotifyTripStartedAsync(
            notification.TripId,
            notification.PassengerId,
            ct);

        await _notificationService.SendPushNotificationAsync(
            notification.PassengerId,
            LocalizationKeys.Notification.TripStartedTitle,
            LocalizationKeys.Notification.TripStartedBody,
            new Dictionary<string, string>
            {
                { "tripId", notification.TripId.ToString() },
                { "status", "InProgress" },
            },
            ct);
    }
}
