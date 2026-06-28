using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetAllTrips;

public class GetAllTripsQueryHandler(IAppDbContext context, TimeProvider timeProvider)
    : IRequestHandler<GetAllTripsQuery, Result<PagedResult<TripDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<PagedResult<TripDto>>> Handle(GetAllTripsQuery request, CancellationToken ct)
    {
        var query = _context.Trips
            .AsNoTracking()
            .Where(t => t.DeletedAtUtc == null)
            .Where(t => t.Status != TripStatus.AwaitingPayment)
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

        if (request.PassengerId.HasValue)
        {
            query = query.Where(t => t.PassengerId == request.PassengerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(t => t.ReferenceCode.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(ct);

        var trips = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var vehicleTypes = await _context.VehicleTypes.AsNoTracking().ToListAsync(ct);
        var vehicleTypeMap = vehicleTypes.ToDictionary(v => v.Id, v => v.Name.En ?? "Unknown");
        var adminNames = await _context.AdminProfiles
            .AsNoTracking()
            .ToDictionaryAsync(a => a.Id, a => a.Name, ct);
        var now = timeProvider.GetUtcNow();
        var tripDtos = new List<TripDto>();

        foreach (var trip in trips)
        {
            var quote = await _context.PricingQuotes
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);
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
                .AsNoTracking()
                .Where(c => c.TripId == trip.Id)
                .OrderByDescending(c => c.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);
            var compensationClaim = await _context.TripCompensationClaims
                .AsNoTracking()
                .Where(c => c.TripId == trip.Id)
                .OrderByDescending(c => c.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);
            var activeWaitingSession = await _context.TripWaitingSessions
                .AsNoTracking()
                .Where(w => w.TripId == trip.Id && w.StoppedAtUtc == null)
                .OrderByDescending(w => w.StartedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (trip.DriverId.HasValue)
            {
                var driver = await _context.Drivers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == trip.DriverId.Value, ct);
                if (driver != null)
                {
                    driverLatitude = driver.CurrentLat.HasValue ? (double)driver.CurrentLat.Value : null;
                    driverLongitude = driver.CurrentLng.HasValue ? (double)driver.CurrentLng.Value : null;
                }
            }

            var acceptedAdminName = trip.AcceptedByAdminId is { } acceptedAdminId &&
                adminNames.TryGetValue(acceptedAdminId, out var resolvedAdminName)
                ? resolvedAdminName
                : null;

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
                CompletedAtUtc: trip.CompletedAtUtc,
                PassengerNote: trip.PassengerNote,
                IsAirport: trip.IsAirport,
                FlightNumber: trip.FlightNumber,
                AcceptedByAdminId: trip.AcceptedByAdminId,
                AcceptedAdminName: acceptedAdminName,
                AcceptedAtUtc: trip.AcceptedAtUtc,
                IsScheduled: trip.ScheduledAtUtc.HasValue,
                DispatchWindowOpensAtUtc: trip.DispatchWindowOpensAtUtc,
                CanMarkEnRoute: trip.CanMarkEnRoute(now),
                AttentionState: trip.GetAttentionState(now).ToString()));
        }

        return new PagedResult<TripDto>(tripDtos, totalCount, request.PageNumber, request.PageSize);
    }
}
