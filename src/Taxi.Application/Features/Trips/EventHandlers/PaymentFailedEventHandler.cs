using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class PaymentFailedEventHandler(ITripNotifier notifier)
    : INotificationHandler<PaymentFailed>
{
    private readonly ITripNotifier _notifier = notifier;

    public Task Handle(PaymentFailed notification, CancellationToken ct) =>
        _notifier.NotifyPaymentFailedAsync(
            notification.TripId,
            notification.PassengerId,
            notification.Reason,
            ct);
}
