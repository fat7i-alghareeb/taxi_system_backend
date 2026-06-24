using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Drivers;

namespace Taxi.Infrastructure.Hubs;

[Authorize]
public sealed class TripHub(IAppDbContext context, ILogger<TripHub> logger) : Hub
{
    public const string HubUrl = "/hubs/trips";
    public const string AdminsGroup = "Admins";

    private readonly IAppDbContext _context = context;
    private readonly ILogger<TripHub> _logger = logger;

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
    public async Task JoinTripGroup(Guid tripId)
    {
        var userId = RequireUserId();
        var isAdmin = Context.User?.IsInRole("Admin") == true;
        var isParticipant = isAdmin || await _context.Trips
            .AsNoTracking()
            .Where(trip => trip.Id == tripId)
            .AnyAsync(
                trip => trip.PassengerId == userId ||
                    (trip.DriverId != null && _context.Drivers.Any(
                        driver => driver.Id == trip.DriverId && driver.UserId == userId)),
                Context.ConnectionAborted);

        if (!isParticipant)
        {
            _logger.LogWarning(
                "[TripHub] Forbidden trip join connectionId={ConnectionId} userId={UserId} tripId={TripId}",
                Context.ConnectionId,
                userId,
                tripId);
            throw new HubException("You are not authorized to subscribe to this trip.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"Trip_{tripId}");
        _logger.LogInformation(
            "[TripHub] JoinTripGroup connectionId={ConnectionId} userId={UserId} group=Trip_{TripId}",
            Context.ConnectionId,
            Context.UserIdentifier ?? "(none)",
            tripId);
    }

    public async Task LeaveTripGroup(Guid tripId)
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
    public async Task JoinVehicleTypeGroup(Guid vehicleTypeId)
    {
        var userId = RequireUserId();
        var canJoin = await _context.Drivers
            .AsNoTracking()
            .AnyAsync(
                driver => driver.UserId == userId &&
                    driver.IsActive &&
                    driver.ApprovalStatus == DriverApprovalStatus.Approved &&
                    driver.VehicleTypeId == vehicleTypeId,
                Context.ConnectionAborted);

        if (!canJoin)
        {
            _logger.LogWarning(
                "[TripHub] Forbidden vehicle group join connectionId={ConnectionId} userId={UserId} vehicleTypeId={VehicleTypeId}",
                Context.ConnectionId,
                userId,
                vehicleTypeId);
            throw new HubException("You are not authorized to subscribe to this vehicle type.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"VehicleType_{vehicleTypeId}");
        _logger.LogInformation(
            "[TripHub] JoinVehicleTypeGroup connectionId={ConnectionId} userId={UserId} group=VehicleType_{VehicleTypeId}",
            Context.ConnectionId,
            userId,
            vehicleTypeId);
    }

    public async Task LeaveVehicleTypeGroup(Guid vehicleTypeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"VehicleType_{vehicleTypeId}");
        _logger.LogInformation(
            "[TripHub] LeaveVehicleTypeGroup connectionId={ConnectionId} userId={UserId} group=VehicleType_{VehicleTypeId}",
            Context.ConnectionId,
            Context.UserIdentifier ?? "(none)",
            vehicleTypeId);
    }

    private Guid RequireUserId()
    {
        if (Guid.TryParse(Context.UserIdentifier, out var userId))
        {
            return userId;
        }

        throw new HubException("The authenticated user identifier is invalid.");
    }
}
