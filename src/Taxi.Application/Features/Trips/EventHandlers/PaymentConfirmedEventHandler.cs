using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class PaymentConfirmedEventHandler(ITripNotifier notifier)
    : INotificationHandler<PaymentConfirmed>
{
    private readonly ITripNotifier _notifier = notifier;

    public Task Handle(PaymentConfirmed notification, CancellationToken ct) =>
        _notifier.NotifyPaymentConfirmedAsync(
            notification.TripId,
            notification.PassengerId,
            ct);
}
