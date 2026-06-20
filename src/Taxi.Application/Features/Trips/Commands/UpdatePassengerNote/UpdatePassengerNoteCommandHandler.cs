using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.UpdatePassengerNote;

public class UpdatePassengerNoteCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<UpdatePassengerNoteCommand, Result<TripDto>>
{
    public async Task<Result<TripDto>> Handle(UpdatePassengerNoteCommand request, CancellationToken ct)
    {
        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (!currentUser.IsAdmin)
        {
            if (!Guid.TryParse(currentUser.Id, out var passengerId))
            {
                return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
            }

            if (trip.PassengerId != passengerId)
            {
                return TripErrors.NotOwnedByPassenger;
            }
        }

        var updateResult = trip.UpdatePassengerNote(request.PassengerNote);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        var quote = await context.PricingQuotes.FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);
        var vehicleType = await context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);
        var cancellation = await context.TripCancellations
            .Where(c => c.TripId == trip.Id)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
        var compensationClaim = await context.TripCompensationClaims
            .Where(c => c.TripId == trip.Id)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
        var activeWaitingSession = await context.TripWaitingSessions
            .Where(w => w.TripId == trip.Id && w.StoppedAtUtc == null)
            .OrderByDescending(w => w.StartedAtUtc)
            .FirstOrDefaultAsync(ct);
        var passenger = await context.DomainUsers.FirstOrDefaultAsync(u => u.Id == trip.PassengerId, ct);

        double? driverLatitude = null;
        double? driverLongitude = null;

        if (trip.DriverId.HasValue)
        {
            var driver = await context.Drivers.FirstOrDefaultAsync(d => d.Id == trip.DriverId.Value, ct);
            if (driver is not null)
            {
                driverLatitude = driver.CurrentLat.HasValue ? (double)driver.CurrentLat.Value : null;
                driverLongitude = driver.CurrentLng.HasValue ? (double)driver.CurrentLng.Value : null;
            }
        }

        var stopDtos = trip.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new TripStopDto(
                s.Coordinate.Latitude,
                s.Coordinate.Longitude,
                s.AddressLabel,
                s.Sequence,
                s.IsCompleted,
                s.CompletedAtUtc))
            .ToList();

        return new TripDto(
            trip.Id,
            trip.ReferenceCode,
            trip.PassengerId,
            trip.DriverId,
            trip.VehicleTypeId,
            trip.Status.ToString(),
            quote?.FinalFare ?? 0,
            quote?.CurrencyCode ?? "EUR",
            trip.CreatedAtUtc,
            trip.ScheduledAtUtc,
            stopDtos,
            null,
            driverLatitude,
            driverLongitude,
            vehicleType?.Name.En ?? "Unknown",
            cancellation?.ToDto(),
            compensationClaim?.ToDto(),
            activeWaitingSession?.ToDto(),
            passenger?.Name,
            passenger?.Phone,
            AssignedAtUtc: trip.AssignedAtUtc,
            ArrivedAtUtc: trip.ArrivedAtUtc,
            StartedAtUtc: trip.StartedAtUtc,
            CompletedAtUtc: trip.CompletedAtUtc,
            PassengerNote: trip.PassengerNote,
            IsAirport: trip.IsAirport,
            FlightNumber: trip.FlightNumber,
            AcceptedByAdminId: trip.AcceptedByAdminId,
            AcceptedAtUtc: trip.AcceptedAtUtc,
            IsScheduled: trip.ScheduledAtUtc.HasValue,
            DispatchWindowOpensAtUtc: trip.DispatchWindowOpensAtUtc,
            CanMarkEnRoute: trip.CanMarkEnRoute(timeProvider.GetUtcNow()),
            AttentionState: trip.GetAttentionState(timeProvider.GetUtcNow()).ToString());
    }
}
