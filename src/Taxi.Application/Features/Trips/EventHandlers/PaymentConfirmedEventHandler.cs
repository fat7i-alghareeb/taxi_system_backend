using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Contracts.Notifications;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class PaymentConfirmedEventHandler(
    ITripNotifier notifier,
    INotificationService notificationService)
    : INotificationHandler<PaymentConfirmed>
{
    public async Task Handle(PaymentConfirmed notification, CancellationToken ct)
    {
        await notifier.NotifyPaymentConfirmedAsync(
            notification.TripId,
            notification.PassengerId,
            ct,
            notification.EventId);

        await notifier.NotifyTripAwaitingAdminAcceptanceAsync(
            notification.TripId,
            notification.VehicleTypeId,
            notification.PassengerId,
            notification.ScheduledAtUtc,
            ct,
            notification.EventId);

        var scheduledLabel = notification.ScheduledAtUtc?.ToString("yyyy-MM-dd HH:mm 'UTC'")
            ?? "now";
        var data = new Dictionary<string, string>
        {
            { "tripId", notification.TripId.ToString() },
            { "status", "AwaitingAdminAcceptance" },
            { "type", "trip_awaiting_admin_acceptance" },
            { "eventId", notification.EventId.ToString() },
        };

        if (notification.ScheduledAtUtc.HasValue)
        {
            data["scheduledAtUtc"] = notification.ScheduledAtUtc.Value.ToString("O");
            await notificationService.SendPushNotificationAsync(
                notification.PassengerId,
                LocalizationKeys.Notification.TripScheduledConfirmedTitle,
                LocalizationKeys.Notification.TripScheduledConfirmedBody,
                data,
                ct,
                bodyArgs: [scheduledLabel]);
        }

        await notificationService.SendPushNotificationToAdminsAsync(
            LocalizationKeys.Notification.AdminScheduledTripTitle,
            LocalizationKeys.Notification.AdminScheduledTripBody,
            data,
            ct,
            bodyArgs: [notification.ReferenceCode, scheduledLabel]);
    }
}
