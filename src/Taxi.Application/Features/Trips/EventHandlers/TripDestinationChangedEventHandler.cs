using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

/// <summary>
/// Notifies the assigned driver (push + realtime re-route), passenger and admins when a trip's
/// drop-off is changed mid-trip. Only raised while a driver is assigned and the drop-off moved.
/// </summary>
public sealed class TripDestinationChangedEventHandler(
    IAppDbContext context,
    ITripNotifier notifier,
    INotificationService notificationService)
    : INotificationHandler<TripDestinationChanged>
{
    public async Task Handle(TripDestinationChanged notification, CancellationToken ct)
    {
        var driverUserId = await ResolveDriverUserIdAsync(notification.DriverId, ct);

        await notifier.NotifyTripDestinationChangedAsync(
            notification.TripId,
            notification.PassengerId,
            driverUserId,
            notification.DriverId,
            notification.NewDropoffLatitude,
            notification.NewDropoffLongitude,
            notification.NewDropoffLabel,
            ct,
            notification.EventId);

        if (driverUserId is not { } driverRecipient)
        {
            return;
        }

        var data = new Dictionary<string, string>
        {
            ["tripId"] = notification.TripId.ToString(),
            ["type"] = "trip_destination_changed",
            ["eventId"] = notification.EventId.ToString(),
        };

        object[] bodyArgs = [notification.NewDropoffLabel ?? string.Empty];

        await notificationService.SendPushNotificationAsync(
            driverRecipient,
            LocalizationKeys.Notification.TripDestinationChangedTitle,
            LocalizationKeys.Notification.TripDestinationChangedBody,
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
