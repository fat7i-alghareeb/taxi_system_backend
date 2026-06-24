using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Notifications;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripMessageRealtimeDeliveryRequestedEventHandler(ITripNotifier notifier)
    : INotificationHandler<TripMessageRealtimeDeliveryRequested>
{
    public Task Handle(TripMessageRealtimeDeliveryRequested notification, CancellationToken ct)
    {
        var payload = new TripMessageNotification(
            notification.TripId,
            notification.MessageId,
            notification.SenderId,
            notification.SenderRole.ToString(),
            notification.Content,
            notification.PhotoUrl,
            notification.SentAtUtc);

        return notifier.NotifyTripMessageAsync(
            payload,
            notification.PassengerId,
            notification.DriverUserId,
            ct);
    }
}
