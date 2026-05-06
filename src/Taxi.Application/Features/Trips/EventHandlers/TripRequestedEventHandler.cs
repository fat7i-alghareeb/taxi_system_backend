using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripRequestedEventHandler(ITripNotifier notifier)
    : INotificationHandler<TripRequested>
{
    private readonly ITripNotifier _notifier = notifier;

    public Task Handle(TripRequested notification, CancellationToken ct) =>
        _notifier.NotifyTripRequestedAsync(
            notification.TripId,
            notification.VehicleTypeId,
            notification.PassengerId,
            ct);
}
