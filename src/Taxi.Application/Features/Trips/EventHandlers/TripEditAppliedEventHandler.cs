using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Contracts.Notifications;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

/// <summary>
/// Tells the passenger their edit went through and what it cost — over realtime for the open app,
/// and by push because the PaymentSheet path commits at the Stripe webhook, by which time the app
/// may well be backgrounded. Admins and the assigned driver ride along on the same realtime event.
/// </summary>
public sealed class TripEditAppliedEventHandler(
    IAppDbContext context,
    ITripNotifier notifier,
    INotificationService notificationService)
    : INotificationHandler<TripEditApplied>
{
    public async Task Handle(TripEditApplied notification, CancellationToken ct)
    {
        var driverUserId = await ResolveDriverUserIdAsync(notification.DriverId, ct);
        var lang = await context.DomainUsers
            .Where(u => u.Id == notification.PassengerId)
            .Select(u => u.PreferredLanguage)
            .FirstOrDefaultAsync(ct) ?? Languages.Default;

        var vehicleTypeName = await context.VehicleTypes
            .Where(v => v.Id == notification.VehicleTypeId)
            .Select(v => lang == Languages.Ar ? v.Name.Ar : (lang == Languages.Nl ? v.Name.Nl : v.Name.En))
            .FirstOrDefaultAsync(ct);

        await notifier.NotifyTripEditAppliedAsync(
            new TripEditAppliedNotification(
                notification.TripId,
                notification.PassengerId,
                notification.DriverId,
                notification.NewFare,
                notification.CurrencyCode,
                notification.Delta,
                notification.PassengerCount,
                vehicleTypeName,
                notification.DropoffLabel,
                notification.StopsChanged,
                notification.PassengerCountChanged,
                notification.EventId),
            driverUserId,
            ct);

        var data = new Dictionary<string, string>
        {
            ["tripId"] = notification.TripId.ToString(),
            ["type"] = "trip_edit_applied",
            ["eventId"] = notification.EventId.ToString(),
        };

        // A price move is what the customer needs to hear about; a same-price edit only warrants
        // the silent realtime refresh.
        var (titleKey, bodyKey) = notification.Delta switch
        {
            > 0m => (LocalizationKeys.Notification.TripEditChargedTitle, LocalizationKeys.Notification.TripEditChargedBody),
            < 0m => (LocalizationKeys.Notification.TripEditRefundedTitle, LocalizationKeys.Notification.TripEditRefundedBody),
            _ => (string.Empty, string.Empty),
        };

        if (titleKey.Length == 0)
        {
            return;
        }

        object[] bodyArgs =
        [
            $"{Math.Abs(notification.Delta):0.00} {notification.CurrencyCode}",
            $"{notification.NewFare:0.00} {notification.CurrencyCode}",
        ];

        await notificationService.SendPushNotificationAsync(
            notification.PassengerId,
            titleKey,
            bodyKey,
            data,
            ct,
            bodyArgs: bodyArgs);
    }

    private async Task<Guid?> ResolveDriverUserIdAsync(Guid? driverId, CancellationToken ct)
    {
        if (driverId is not { } id)
        {
            return null;
        }

        return await context.Drivers
            .Where(d => d.Id == id)
            .Select(d => (Guid?)d.UserId)
            .FirstOrDefaultAsync(ct);
    }
}
