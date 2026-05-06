using Microsoft.AspNetCore.SignalR;

namespace Taxi.Infrastructure.Hubs;

public class TripHub : Hub
{
    public async Task JoinTripGroup(string tripId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
    }

    public async Task LeaveTripGroup(string tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
    }
}
