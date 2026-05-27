using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripCompletedEventHandler(
    ITripNotifier notifier,
    INotificationService notificationService)
    : INotificationHandler<TripCompleted>
{
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;

    public async Task Handle(TripCompleted notification, CancellationToken ct)
    {
        await _notifier.NotifyTripCompletedAsync(
            notification.TripId,
            notification.PassengerId,
            ct);

        await _notificationService.SendPushNotificationAsync(
            notification.PassengerId,
            LocalizationKeys.Notification.TripCompletedTitle,
            LocalizationKeys.Notification.TripCompletedBody,
            new Dictionary<string, string>
            {
                { "tripId", notification.TripId.ToString() },
                { "status", "Completed" },
            },
            ct);
    }
}
