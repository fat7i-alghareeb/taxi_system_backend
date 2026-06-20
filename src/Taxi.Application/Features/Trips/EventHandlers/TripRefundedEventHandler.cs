using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripRefundedEventHandler(ITripNotifier notifier)
    : INotificationHandler<TripRefunded>
{
    private readonly ITripNotifier _notifier = notifier;

    public Task Handle(TripRefunded notification, CancellationToken ct) =>
        _notifier.NotifyTripRefundedAsync(
            notification.TripId,
            notification.PassengerId,
            notification.Amount,
            ct,
            notification.EventId);
}
