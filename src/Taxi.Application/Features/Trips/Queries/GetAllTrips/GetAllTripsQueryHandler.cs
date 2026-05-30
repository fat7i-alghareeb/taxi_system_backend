using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetAllTrips;

public class GetAllTripsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAllTripsQuery, Result<List<TripDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<TripDto>>> Handle(GetAllTripsQuery request, CancellationToken ct)
    {
        var query = _context.Trips
            .Where(t => t.DeletedAtUtc == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<TripStatus>(request.Status, true, out var tripStatus))
            {
                query = query.Where(t => t.Status == tripStatus);
            }
        }

        if (request.DriverId.HasValue)
        {
            query = query.Where(t => t.DriverId == request.DriverId.Value);
        }

        var trips = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var vehicleTypes = await _context.VehicleTypes.ToListAsync(ct);
        var vehicleTypeMap = vehicleTypes.ToDictionary(v => v.Id, v => v.Name.En ?? "Unknown");
        var tripDtos = new List<TripDto>();

        foreach (var trip in trips)
        {
            var quote = await _context.PricingQuotes.FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);
            var fare = quote?.FinalFare ?? 0;
            var currency = quote?.CurrencyCode ?? "EUR";

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

            double? driverLatitude = null;
            double? driverLongitude = null;

            vehicleTypeMap.TryGetValue(trip.VehicleTypeId, out var vehicleTypeName);
            vehicleTypeName ??= "Unknown";
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

            tripDtos.Add(new TripDto(
                trip.Id,
                trip.ReferenceCode,
                trip.PassengerId,
                trip.DriverId,
                trip.VehicleTypeId,
                trip.Status.ToString(),
                fare,
                currency,
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
                AssignedAtUtc: trip.AssignedAtUtc,
                ArrivedAtUtc: trip.ArrivedAtUtc,
                StartedAtUtc: trip.StartedAtUtc,
                CompletedAtUtc: trip.CompletedAtUtc));
        }

        return tripDtos;
    }
}
