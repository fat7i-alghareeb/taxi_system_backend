using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Drivers;
using Taxi.Domain.Trips;

namespace Taxi.Infrastructure.Hubs;

[Authorize]
public sealed class LocationTrackingHub(IAppDbContext context, IHubContext<TripHub> tripHubContext) : Hub
{
    private readonly IAppDbContext _context = context;
    private readonly IHubContext<TripHub> _tripHubContext = tripHubContext;

    public override async Task OnConnectedAsync()
    {
        if (Context.User != null && Context.User.IsInRole("Admin"))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }

        await base.OnConnectedAsync();
    }

    public async Task UpdateLocation(double lat, double lng)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return;
        }

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userGuid);
        if (driver == null)
        {
            return;
        }

        var updateResult = driver.UpdateLocation((decimal)lat, (decimal)lng);
        if (updateResult.IsSuccess)
        {
            await _context.SaveChangesAsync(default);

            // 1. Broadcast to Admins God-Mode Group
            await Clients.Group("Admins").SendAsync("DriverLocationUpdated", new
            {
                DriverId = driver.Id,
                Latitude = lat,
                Longitude = lng,
                Status = driver.Status.ToString()
            });

            // 2. Broadcast to Passenger (if driver has an active trip)
            var activeTrip = await _context.Trips.FirstOrDefaultAsync(t =>
                t.DriverId == driver.Id &&
                (t.Status == TripStatus.Accepted ||
                 t.Status == TripStatus.EnRoute ||
                 t.Status == TripStatus.Arrived ||
                 t.Status == TripStatus.InProgress));

            if (activeTrip != null)
            {
                await _tripHubContext.Clients.Group($"Trip_{activeTrip.Id}").SendAsync("DriverLocationUpdated", new
                {
                    TripId = activeTrip.Id,
                    DriverId = driver.Id,
                    Latitude = lat,
                    Longitude = lng
                });
            }
        }
    }
}
