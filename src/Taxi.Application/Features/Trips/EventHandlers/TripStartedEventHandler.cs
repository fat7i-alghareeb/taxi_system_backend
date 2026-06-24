using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripStartedEventHandler(
    ITripNotifier notifier,
    IAppDbContext context)
    : INotificationHandler<TripStarted>
{
    private readonly ITripNotifier _notifier = notifier;
    private readonly IAppDbContext _context = context;

    public async Task Handle(TripStarted notification, CancellationToken ct)
    {
        // Realtime (SignalR) update only; no push notification is sent to the passenger
        // when the trip starts. See DriverAssignedEventHandler for rationale.
        var driverId = await _context.Trips
            .Where(trip => trip.Id == notification.TripId)
            .Select(trip => trip.DriverId)
            .FirstOrDefaultAsync(ct);
        var driverUserId = driverId is { } id
            ? await _context.Drivers
                .Where(driver => driver.Id == id)
                .Select(driver => (Guid?)driver.UserId)
                .FirstOrDefaultAsync(ct)
            : null;

        await _notifier.NotifyTripStartedAsync(
            notification.TripId,
            notification.PassengerId,
            driverUserId,
            ct,
            notification.EventId);
    }
}
