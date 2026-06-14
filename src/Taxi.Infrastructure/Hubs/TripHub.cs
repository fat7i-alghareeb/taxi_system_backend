using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Taxi.Infrastructure.Hubs;

[Authorize]
public sealed class TripHub : Hub
{
    public const string HubUrl = "/hubs/trips";
    public const string AdminsGroup = "Admins";

    private readonly ILogger<TripHub> _logger;

    public TripHub(ILogger<TripHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var isAdmin = Context.User?.IsInRole("Admin") == true;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        // Auto-join the Admins group so admins receive TripRequested broadcasts.
        if (isAdmin)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
        }

        _logger.LogInformation(
            "[TripHub] Connected connectionId={ConnectionId} userId={UserId} isAdmin={IsAdmin} joinedGroups=User_{UserId}{AdminGroup}",
            Context.ConnectionId,
            userId ?? "(none)",
            isAdmin,
            userId ?? "(none)",
            isAdmin ? ",Admins" : string.Empty);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            exception,
            "[TripHub] Disconnected connectionId={ConnectionId} userId={UserId} reason={Reason}",
            Context.ConnectionId,
            Context.UserIdentifier ?? "(none)",
            exception?.Message ?? "(clean)");

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Called by a passenger or driver to receive updates for a specific trip.</summary>
    /// <param name="tripId">The trip identifier used to form the group name.</param>
    public async Task JoinTripGroup(string tripId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
        _logger.LogInformation(
            "[TripHub] JoinTripGroup connectionId={ConnectionId} userId={UserId} group=Trip_{TripId}",
            Context.ConnectionId,
            Context.UserIdentifier ?? "(none)",
            tripId);
    }

    public async Task LeaveTripGroup(string tripId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
        _logger.LogInformation(
            "[TripHub] LeaveTripGroup connectionId={ConnectionId} userId={UserId} group=Trip_{TripId}",
            Context.ConnectionId,
            Context.UserIdentifier ?? "(none)",
            tripId);
    }

    /// <summary>Called by drivers on app startup to receive new trip requests for their vehicle type.</summary>
    /// <param name="vehicleTypeCode">The vehicle type code used to form the group name.</param>
    public async Task JoinVehicleTypeGroup(string vehicleTypeCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"VehicleType_{vehicleTypeCode}");
        _logger.LogInformation(
            "[TripHub] JoinVehicleTypeGroup connectionId={ConnectionId} userId={UserId} group=VehicleType_{VehicleTypeCode}",
            Context.ConnectionId,
            Context.UserIdentifier ?? "(none)",
            vehicleTypeCode);
    }

    public async Task LeaveVehicleTypeGroup(string vehicleTypeCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"VehicleType_{vehicleTypeCode}");
        _logger.LogInformation(
            "[TripHub] LeaveVehicleTypeGroup connectionId={ConnectionId} userId={UserId} group=VehicleType_{VehicleTypeCode}",
            Context.ConnectionId,
            Context.UserIdentifier ?? "(none)",
            vehicleTypeCode);
    }
}
