using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.CancelTrip;

public class CancelTripCommandHandler(
    IAppDbContext context,
    IUser currentUser) : IRequestHandler<CancelTripCommand, Result<TripDto>>
{
    public async Task<Result<TripDto>> Handle(CancelTripCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);

        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (trip.PassengerId != passengerId)
        {
            return TripErrors.NotOwnedByPassenger;
        }

        if (trip.Status == TripStatus.InProgress || trip.Status == TripStatus.Completed || trip.Status == TripStatus.Cancelled)
        {
            return TripErrors.CannotCancel;
        }

        var cancelResult = trip.Cancel();
        if (cancelResult.IsError)
        {
            return cancelResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        var quote = await context.PricingQuotes.FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);

        var stopDtos = trip.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new TripStopDto(s.Coordinate.Latitude, s.Coordinate.Longitude))
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
            stopDtos);
    }
}
