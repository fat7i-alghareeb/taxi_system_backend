using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.StartTripWaiting;

public sealed class StartTripWaitingCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<StartTripWaitingCommand, Result<WaitingSessionDto>>
{
    public async Task<Result<WaitingSessionDto>> Handle(StartTripWaitingCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var driverUserId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        // Admins can act on any trip; drivers only on trips assigned to them.
        if (!currentUser.IsAdmin)
        {
            var driver = await context.Drivers.FirstOrDefaultAsync(d => d.UserId == driverUserId, ct);
            if (driver is null)
            {
                return TripErrors.DriverNotFound;
            }

            if (trip.DriverId != driver.Id)
            {
                return Error.Validation(LocalizationKeys.Trip.DriverMismatch, "This trip is not assigned to you.");
            }
        }

        if (trip.Status != TripStatus.DriverArrived)
        {
            return TripErrors.InvalidStatus(trip.Status);
        }

        var hasActive = await context.TripWaitingSessions.AnyAsync(s => s.TripId == trip.Id && s.StoppedAtUtc == null, ct);
        if (hasActive)
        {
            return TripErrors.ActiveWaitingSessionExists;
        }

        if (trip.DriverId is null)
        {
            return Error.Validation(LocalizationKeys.Trip.DriverMismatch, "Trip has no assigned driver.");
        }

        var vehicleType = await context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);
        var ratePerMinute = vehicleType?.RatePerMin ?? TripWaitingSession.DefaultFeePerMinute;

        var sessionResult = TripWaitingSession.Start(Guid.NewGuid(), trip.Id, trip.DriverId.Value, ratePerMinute);
        if (sessionResult.IsError)
        {
            return sessionResult.Errors;
        }

        context.TripWaitingSessions.Add(sessionResult.Value);
        await context.SaveChangesAsync(ct);
        return sessionResult.Value.ToDto();
    }
}
