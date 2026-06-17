using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripCancelledEventHandler(
    ITripNotifier notifier,
    INotificationService notificationService,
    IAppDbContext context)
    : INotificationHandler<TripCancelled>
{
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAppDbContext _context = context;

    public async Task Handle(TripCancelled notification, CancellationToken ct)
    {
        var data = new Dictionary<string, string>
        {
            { "tripId", notification.TripId.ToString() },
            { "status", "Cancelled" },
        };

        // 1. Passenger (SignalR trip/user groups + push).
        await _notifier.NotifyTripCancelledAsync(
            notification.TripId,
            notification.PassengerId,
            ct);

        await _notificationService.SendPushNotificationAsync(
            notification.PassengerId,
            LocalizationKeys.Notification.TripCancelledTitle,
            LocalizationKeys.Notification.TripCancelledBody,
            data,
            ct);

        // 2. Admins (live dashboard update).
        await _notifier.NotifyTripCancelledToAdminsAsync(
            notification.TripId,
            notification.PassengerId,
            ct);

        // 3. Assigned driver, if any — so they stop heading to / waiting at pickup.
        //    The event carries the Driver entity id; resolve the backing user id for
        //    the per-user SignalR group and FCM token lookup.
        if (notification.DriverId is { } driverId)
        {
            var driverUserId = await _context.Drivers
                .Where(d => d.Id == driverId)
                .Select(d => (Guid?)d.UserId)
                .FirstOrDefaultAsync(ct);

            if (driverUserId is { } resolvedDriverUserId)
            {
                await _notifier.NotifyTripCancelledToDriverAsync(
                    notification.TripId,
                    resolvedDriverUserId,
                    notification.PassengerId,
                    ct);

                await _notificationService.SendPushNotificationAsync(
                    resolvedDriverUserId,
                    LocalizationKeys.Notification.TripCancelledTitle,
                    LocalizationKeys.Notification.TripCancelledBody,
                    data,
                    ct);
            }
        }
    }
}
