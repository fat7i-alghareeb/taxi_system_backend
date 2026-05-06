using Microsoft.AspNetCore.SignalR;

namespace Taxi.Infrastructure.Hubs;

public sealed class TripHub : Hub
{
    public const string HubUrl = "/hubs/trips";

    /// <summary>Called by a passenger or driver to receive updates for a specific trip.</summary>
    public async Task JoinTripGroup(string tripId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
    }

    public async Task LeaveTripGroup(string tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
    }

    /// <summary>Called by drivers on app startup to receive new trip requests for their vehicle type.</summary>
    public async Task JoinVehicleTypeGroup(string vehicleTypeCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"VehicleType_{vehicleTypeCode}");
    }

    public async Task LeaveVehicleTypeGroup(string vehicleTypeCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"VehicleType_{vehicleTypeCode}");
    }
}
