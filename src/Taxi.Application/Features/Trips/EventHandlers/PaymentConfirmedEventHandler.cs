using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class PaymentConfirmedEventHandler(
    ITripNotifier notifier,
    INotificationService notificationService)
    : INotificationHandler<PaymentConfirmed>
{
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;

    public async Task Handle(PaymentConfirmed notification, CancellationToken ct)
    {
        await _notifier.NotifyPaymentConfirmedAsync(
            notification.TripId,
            notification.PassengerId,
            ct);

        if (notification.ScheduledAtUtc.HasValue)
        {
            await _notificationService.SendPushNotificationAsync(
                notification.PassengerId,
                LocalizationKeys.Notification.TripScheduledConfirmedTitle,
                LocalizationKeys.Notification.TripScheduledConfirmedBody,
                new Dictionary<string, string>
                {
                    { "tripId", notification.TripId.ToString() },
                    { "status", "Scheduled" },
                },
                ct);
        }
    }
}
