using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class DriverAssignedEventHandler(ITripNotifier notifier)
    : INotificationHandler<DriverAssigned>
{
    private readonly ITripNotifier _notifier = notifier;

    public async Task Handle(DriverAssigned notification, CancellationToken ct)
    {
        // Realtime (SignalR) update keeps the app UI in sync, but no push notification
        // is sent to the passenger for this stage. The passenger only receives the three
        // approved push notifications: en-route, arrived and completed.
        await _notifier.NotifyDriverAssignedAsync(
            notification.TripId,
            notification.PassengerId,
            notification.DriverId,
            ct,
            notification.EventId);
    }
}
