using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripCancelledEventHandler(ITripNotifier notifier)
    : INotificationHandler<TripCancelled>
{
    private readonly ITripNotifier _notifier = notifier;

    public Task Handle(TripCancelled notification, CancellationToken ct) =>
        _notifier.NotifyTripCancelledAsync(
            notification.TripId,
            notification.PassengerId,
            ct);
}

