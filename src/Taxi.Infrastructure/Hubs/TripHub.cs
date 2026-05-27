using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Taxi.Infrastructure.Hubs;

[Authorize]
public sealed class TripHub : Hub
{
    public const string HubUrl = "/hubs/trips";
    public const string AdminsGroup = "Admins";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        // Auto-join the Admins group so admins receive TripRequested broadcasts.
        if (Context.User?.IsInRole("Admin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>Called by a passenger or driver to receive updates for a specific trip.</summary>
    /// <param name="tripId">The trip identifier used to form the group name.</param>
    public async Task JoinTripGroup(string tripId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
    }

    public async Task LeaveTripGroup(string tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
    }

    /// <summary>Called by drivers on app startup to receive new trip requests for their vehicle type.</summary>
    /// <param name="vehicleTypeCode">The vehicle type code used to form the group name.</param>
    public async Task JoinVehicleTypeGroup(string vehicleTypeCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"VehicleType_{vehicleTypeCode}");
    }

    public async Task LeaveVehicleTypeGroup(string vehicleTypeCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"VehicleType_{vehicleTypeCode}");
    }
}
