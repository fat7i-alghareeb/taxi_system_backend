using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripStopCompletedEventHandler(ITripNotifier notifier)
    : INotificationHandler<TripStopCompleted>
{
    private readonly ITripNotifier _notifier = notifier;

    public Task Handle(TripStopCompleted notification, CancellationToken ct) =>
        _notifier.NotifyTripStopCompletedAsync(
            notification.TripId,
            notification.PassengerId,
            notification.DriverId,
            notification.Sequence,
            ct,
            notification.EventId);
}
