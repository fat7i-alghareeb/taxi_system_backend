using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripCompletedEventHandler(ITripNotifier notifier)
    : INotificationHandler<TripCompleted>
{
    private readonly ITripNotifier _notifier = notifier;

    public Task Handle(TripCompleted notification, CancellationToken ct) =>
        _notifier.NotifyTripCompletedAsync(
            notification.TripId,
            notification.PassengerId,
            ct);
}

