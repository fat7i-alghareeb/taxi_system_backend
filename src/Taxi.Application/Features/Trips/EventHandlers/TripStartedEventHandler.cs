using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripStartedEventHandler(ITripNotifier notifier)
    : INotificationHandler<TripStarted>
{
    private readonly ITripNotifier _notifier = notifier;

    public Task Handle(TripStarted notification, CancellationToken ct) =>
        _notifier.NotifyTripStartedAsync(
            notification.TripId,
            notification.PassengerId,
            ct);
}
