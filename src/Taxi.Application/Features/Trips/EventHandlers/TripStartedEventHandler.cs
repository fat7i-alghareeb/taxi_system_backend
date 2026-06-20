using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripStartedEventHandler(ITripNotifier notifier)
    : INotificationHandler<TripStarted>
{
    private readonly ITripNotifier _notifier = notifier;

    public async Task Handle(TripStarted notification, CancellationToken ct)
    {
        // Realtime (SignalR) update only; no push notification is sent to the passenger
        // when the trip starts. See DriverAssignedEventHandler for rationale.
        await _notifier.NotifyTripStartedAsync(
            notification.TripId,
            notification.PassengerId,
            ct,
            notification.EventId);
    }
}
