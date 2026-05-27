using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetTripById;

public class GetTripByIdQueryHandler(
    IAppDbContext context) : IRequestHandler<GetTripByIdQuery, Result<TripDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<TripDto>> Handle(GetTripByIdQuery request, CancellationToken ct)
    {
        var trip = await _context.Trips
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct);

        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        var quote = await _context.PricingQuotes
            .FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);

        var stopDtos = trip.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new TripStopDto(
                s.Coordinate.Latitude,
                s.Coordinate.Longitude,
                s.AddressLabel))
            .ToList();

        double? driverLatitude = null;
        double? driverLongitude = null;

        var vehicleType = await _context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);
        var vehicleTypeName = vehicleType?.Name.En ?? "Unknown";
        var cancellation = await _context.TripCancellations
            .Where(c => c.TripId == trip.Id)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
        var compensationClaim = await _context.TripCompensationClaims
            .Where(c => c.TripId == trip.Id)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
        var activeWaitingSession = await _context.TripWaitingSessions
            .Where(w => w.TripId == trip.Id && w.StoppedAtUtc == null)
            .OrderByDescending(w => w.StartedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (trip.DriverId.HasValue)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == trip.DriverId.Value, ct);
            if (driver != null)
            {
                driverLatitude = driver.CurrentLat.HasValue ? (double)driver.CurrentLat.Value : null;
                driverLongitude = driver.CurrentLng.HasValue ? (double)driver.CurrentLng.Value : null;
            }
        }

        var passenger = await _context.DomainUsers
            .FirstOrDefaultAsync(u => u.Id == trip.PassengerId, ct);

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
            vehicleTypeName,
            cancellation?.ToDto(),
            compensationClaim?.ToDto(),
            activeWaitingSession?.ToDto(),
            passenger?.Name,
            passenger?.Phone);
    }
}
