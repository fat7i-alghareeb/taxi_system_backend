using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.ArriveTrip;

public class ArriveTripCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<ArriveTripCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(ArriveTripCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var adminUserId))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Auth.Unauthorized, "Unauthorized user."));
        }

        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Trip.NotFound, "Trip not found."));
        }

        if (!currentUser.IsAdmin)
        {
            return Error.Forbidden(LocalizationKeys.Auth.Unauthorized, "Only admins can operate trips.");
        }

        if (trip.AcceptedByAdminId != adminUserId)
        {
            return TripErrors.NotAcceptedByCurrentAdmin;
        }

        var now = timeProvider.GetUtcNow();
        var transitionResult = trip.DriverArrived(now);
        if (transitionResult.IsFailure)
        {
            return transitionResult.Error;
        }

        // Auto-start the waiting meter the moment the driver arrives. The first
        // TripWaitingSession.GraceMinutes are free; beyond that the passenger is
        // billed per minute at the vehicle type's RatePerMin (settled on completion).
        var operatorId = trip.DriverId ?? trip.AcceptedByAdminId;
        if (operatorId is { } driverId)
        {
            var hasActive = await _context.TripWaitingSessions
                .AnyAsync(s => s.TripId == trip.Id && s.StoppedAtUtc == null, ct);
            if (!hasActive)
            {
                var vehicleType = await _context.VehicleTypes
                    .FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);
                var ratePerMinute = vehicleType?.RatePerMin ?? TripWaitingSession.DefaultFeePerMinute;
                var graceMinutes = trip.IsAirport
                    ? TripWaitingSession.AirportGraceMinutes
                    : TripWaitingSession.DefaultGraceMinutes;
                var effectiveWaitingStart = trip.ScheduledAtUtc is { } scheduledAt && scheduledAt > now
                    ? scheduledAt
                    : now;

                var sessionResult = TripWaitingSession.Start(
                    Guid.NewGuid(),
                    trip.Id,
                    driverId,
                    ratePerMinute,
                    graceMinutes,
                    startedAtUtc: effectiveWaitingStart);
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
