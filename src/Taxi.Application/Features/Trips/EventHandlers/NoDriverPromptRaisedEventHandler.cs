using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class NoDriverPromptRaisedEventHandler(
    ITripNotifier notifier,
    INotificationService notificationService)
    : INotificationHandler<NoDriverPromptRaised>
{
    public async Task Handle(NoDriverPromptRaised notification, CancellationToken ct)
    {
        await notifier.NotifyNoDriverFoundAsync(
            notification.TripId,
            notification.PassengerId,
            ct,
            notification.EventId);

        var data = new Dictionary<string, string>
        {
            { "tripId", notification.TripId.ToString() },
            { "type", "no_driver_found" },
            { "eventId", notification.EventId.ToString() },
        };

        await notificationService.SendPushNotificationAsync(
            notification.PassengerId,
            LocalizationKeys.Notification.NoDriverFoundTitle,
            LocalizationKeys.Notification.NoDriverFoundBody,
            data,
            ct);
    }
}
