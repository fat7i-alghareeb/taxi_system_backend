using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.UpdateDriverLocation;

public class UpdateDriverLocationCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IDriverLocationNotifier locationNotifier)
    : IRequestHandler<UpdateDriverLocationCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(UpdateDriverLocationCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var driverUserId))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Auth.Unauthorized, "Unauthorized user."));
        }

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == driverUserId, ct);
        if (driver == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Driver.NotFound, "Driver profile not found."));
        }

        var updateResult = driver.UpdateLocation((decimal)request.Latitude, (decimal)request.Longitude);
        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        // Broadcast to the customer only once the driver is moving toward them (EnRoute+),
        // mirroring LocationTrackingHub so the car appears only after the trip starts moving.
        var activeTripId = await _context.Trips
            .Where(trip => trip.DriverId == driver.Id &&
                (trip.Status == Taxi.Domain.Trips.TripStatus.EnRoute ||
                 trip.Status == Taxi.Domain.Trips.TripStatus.Arrived ||
                 trip.Status == Taxi.Domain.Trips.TripStatus.InProgress))
            .Select(trip => (Guid?)trip.Id)
            .FirstOrDefaultAsync(ct);

        // The realtime SignalR hub is the primary path and carries the live arrival ETA/distance;
        // this REST fallback only refreshes position, so the estimates are left for the next push.
        await locationNotifier.NotifyLocationUpdatedAsync(
            driver.Id,
            request.Latitude,
            request.Longitude,
            driver.Status.ToString(),
            activeTripId,
            null,
            null,
            null,
            ct);

        return Result.Success;
    }
}
