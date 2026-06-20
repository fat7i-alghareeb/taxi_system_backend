using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.StartTripWaiting;

public sealed class StartTripWaitingCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    TimeProvider timeProvider)
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

        if (!currentUser.IsAdmin)
        {
            return Error.Forbidden(LocalizationKeys.Auth.Unauthorized, "Only admins can operate trips.");
        }

        if (trip.AcceptedByAdminId != driverUserId)
        {
            return TripErrors.NotAcceptedByCurrentAdmin;
        }

        if (trip.Status != TripStatus.Arrived)
        {
            return TripErrors.InvalidStatus(trip.Status);
        }

        var hasActive = await context.TripWaitingSessions.AnyAsync(s => s.TripId == trip.Id && s.StoppedAtUtc == null, ct);
        if (hasActive)
        {
            return TripErrors.ActiveWaitingSessionExists;
        }

        var operatorId = trip.DriverId ?? trip.AcceptedByAdminId;
        if (operatorId is null)
        {
            return Error.Validation(LocalizationKeys.Trip.DriverMismatch, "Trip has no assigned driver.");
        }

        var vehicleType = await context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);
        var ratePerMinute = vehicleType?.RatePerMin ?? TripWaitingSession.DefaultFeePerMinute;
        var graceMinutes = trip.IsAirport
            ? TripWaitingSession.AirportGraceMinutes
            : TripWaitingSession.DefaultGraceMinutes;
        var now = timeProvider.GetUtcNow();
        var effectiveWaitingStart = trip.ScheduledAtUtc is { } scheduledAt && scheduledAt > now
            ? scheduledAt
            : now;

        var sessionResult = TripWaitingSession.Start(
            Guid.NewGuid(),
            trip.Id,
            operatorId.Value,
            ratePerMinute,
            graceMinutes,
            effectiveWaitingStart);
        if (sessionResult.IsError)
        {
            return sessionResult.Errors;
        }

        context.TripWaitingSessions.Add(sessionResult.Value);
        await context.SaveChangesAsync(ct);
        return sessionResult.Value.ToDto();
    }
}
