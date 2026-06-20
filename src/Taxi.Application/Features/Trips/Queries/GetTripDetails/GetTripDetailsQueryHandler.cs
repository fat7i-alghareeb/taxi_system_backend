using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetTripDetails;

public class GetTripDetailsQueryHandler(IAppDbContext context, TimeProvider timeProvider)
    : IRequestHandler<GetTripDetailsQuery, Result<TripDetailsDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<TripDetailsDto>> Handle(GetTripDetailsQuery request, CancellationToken ct)
    {
        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId && t.DeletedAtUtc == null, ct);
        if (trip == null)
        {
            return Result.Failure<TripDetailsDto>(Error.NotFound("Trip.NotFound", "Trip not found."));
        }

        var passenger = await _context.DomainUsers.FirstOrDefaultAsync(u => u.Id == trip.PassengerId, ct);
        var passengerName = passenger?.Name ?? "Unknown";
        var passengerPhone = passenger?.Phone ?? string.Empty;

        string? driverName = null;
        string? driverPhone = null;

        if (trip.DriverId.HasValue)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == trip.DriverId.Value, ct);
            if (driver != null)
            {
                var driverUser = await _context.DomainUsers.FirstOrDefaultAsync(u => u.Id == driver.UserId, ct);
                driverName = driverUser?.Name;
                driverPhone = driverUser?.Phone;
            }
        }

        var vehicleType = await _context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);
        var vehicleTypeName = vehicleType?.Name.En ?? "Unknown";

        var quote = await _context.PricingQuotes.FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);
        var fare = quote?.FinalFare ?? 0;
        var currency = quote?.CurrencyCode ?? "EUR";

        var stops = trip.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new TripStopDto(
                s.Coordinate.Latitude,
                s.Coordinate.Longitude,
                s.AddressLabel,
                s.Sequence,
                s.IsCompleted,
                s.CompletedAtUtc))
            .ToList();

        var cancellation = await _context.TripCancellations
            .Where(c => c.TripId == trip.Id)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        var claim = await _context.TripCompensationClaims
            .Where(c => c.TripId == trip.Id)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        var waitingSession = await _context.TripWaitingSessions
            .Where(s => s.TripId == trip.Id && s.StoppedAtUtc == null)
            .OrderByDescending(s => s.StartedAtUtc)
            .FirstOrDefaultAsync(ct);

        // Aggregate every session (active + settled) so the accrued waiting fee
        // remains visible after the session has stopped.
        var allWaitingSessions = await _context.TripWaitingSessions
            .Where(s => s.TripId == trip.Id)
            .ToListAsync(ct);
        var waitingFeeTotal = allWaitingSessions.Sum(s => s.EstimatedFee ?? 0m);
        var waitingBillableMinutes = allWaitingSessions.Sum(s => s.BillableMinutes ?? 0);

        var tripRoute = await _context.TripRoutes
            .FirstOrDefaultAsync(r => r.TripId == trip.Id, ct);
        var acceptedAdminName = trip.AcceptedByAdminId.HasValue
            ? await _context.AdminProfiles
                .Where(a => a.Id == trip.AcceptedByAdminId.Value)
                .Select(a => a.Name)
                .FirstOrDefaultAsync(ct)
            : null;
        var now = timeProvider.GetUtcNow();

        return new TripDetailsDto(
            trip.Id,
            trip.ReferenceCode,
            trip.PassengerId,
            passengerPhone,
            passengerName,
            trip.DriverId,
            driverName,
            driverPhone,
            trip.VehicleTypeId,
            vehicleTypeName,
            trip.Status.ToString(),
            fare,
            currency,
            trip.CreatedAtUtc,
            trip.ScheduledAtUtc,
            trip.AssignedAtUtc,
            trip.ArrivedAtUtc,
            trip.StartedAtUtc,
            trip.CompletedAtUtc,
            stops,
            cancellation?.ToDto(),
            claim?.ToDto(),
            waitingSession?.ToDto(),
            EncodedOverviewPolyline: tripRoute?.EncodedPolyline,
            RouteSegments: TripRouteSegmentMapper.FromJson(tripRoute?.SegmentsJson),
            PassengerNote: trip.PassengerNote,
            IsAirport: trip.IsAirport,
            FlightNumber: trip.FlightNumber,
            WaitingFeeTotal: waitingFeeTotal,
            WaitingBillableMinutes: waitingBillableMinutes,
            AcceptedByAdminId: trip.AcceptedByAdminId,
            AcceptedAdminName: acceptedAdminName,
            AcceptedAtUtc: trip.AcceptedAtUtc,
            IsScheduled: trip.ScheduledAtUtc.HasValue,
            DispatchWindowOpensAtUtc: trip.DispatchWindowOpensAtUtc,
            CanMarkEnRoute: trip.CanMarkEnRoute(now),
            AttentionState: trip.GetAttentionState(now).ToString());
    }
}
