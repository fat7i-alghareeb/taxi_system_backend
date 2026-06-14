using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.ArriveTrip;

public class ArriveTripCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<ArriveTripCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(ArriveTripCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var driverUserId))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Auth.Unauthorized, "Unauthorized user."));
        }

        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Trip.NotFound, "Trip not found."));
        }

        // Admins can act on any trip; drivers only on trips assigned to them.
        if (!currentUser.IsAdmin)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == driverUserId, ct);
            if (driver == null)
            {
                return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Driver.NotFound, "Driver profile not found."));
            }

            if (trip.DriverId != driver.Id)
            {
                return Result.Failure<Success>(Error.Validation(LocalizationKeys.Trip.DriverMismatch, "This trip is not assigned to you."));
            }
        }

        var transitionResult = trip.DriverArrived();
        if (transitionResult.IsFailure)
        {
            return transitionResult.Error;
        }

        // Auto-start the waiting meter the moment the driver arrives. The first
        // TripWaitingSession.GraceMinutes are free; beyond that the passenger is
        // billed per minute at the vehicle type's RatePerMin (settled on completion).
        if (trip.DriverId is { } driverId)
        {
            var hasActive = await _context.TripWaitingSessions
                .AnyAsync(s => s.TripId == trip.Id && s.StoppedAtUtc == null, ct);
            if (!hasActive)
            {
                var vehicleType = await _context.VehicleTypes
                    .FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);
                var ratePerMinute = vehicleType?.RatePerMin ?? TripWaitingSession.DefaultFeePerMinute;

                var sessionResult = TripWaitingSession.Start(Guid.NewGuid(), trip.Id, driverId, ratePerMinute);
                if (!sessionResult.IsError)
                {
                    _context.TripWaitingSessions.Add(sessionResult.Value);
                }
            }
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }
}
