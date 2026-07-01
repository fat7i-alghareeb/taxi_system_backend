using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Dtos;

/// <summary>
/// Builds the full <see cref="TripDto"/> for a loaded <see cref="Trip"/>, pulling
/// in quote, stops, vehicle type, driver location, policies and route. Shared by
/// the get-by-id and get-active-trip query handlers so the shape stays identical.
/// </summary>
public static class TripDtoBuilder
{
    public static async Task<TripDto> BuildAsync(
        IAppDbContext context,
        Trip trip,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var quote = await context.PricingQuotes
            .FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);

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

        var vehicleType = await context.VehicleTypes.FirstOrDefaultAsync(v => v.Id == trip.VehicleTypeId, ct);
        var vehicleTypeName = vehicleType?.Name.En ?? "Unknown";
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

        var allWaitingSessions = await context.TripWaitingSessions
            .Where(w => w.TripId == trip.Id)
            .ToListAsync(ct);
        var waitingFeeTotal = allWaitingSessions.Sum(w => w.EstimatedFee ?? 0m);
        var waitingBillableMinutes = allWaitingSessions.Sum(w => w.BillableMinutes ?? 0);

        var tripRoute = await context.TripRoutes
            .FirstOrDefaultAsync(r => r.TripId == trip.Id, ct);

        var latestRefund = await context.PaymentRefunds
            .Where(r => r.TripId == trip.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (trip.DriverId.HasValue)
        {
            var driver = await context.Drivers.FirstOrDefaultAsync(d => d.Id == trip.DriverId.Value, ct);
            if (driver != null)
            {
                driverLatitude = driver.CurrentLat.HasValue ? (double)driver.CurrentLat.Value : null;
                driverLongitude = driver.CurrentLng.HasValue ? (double)driver.CurrentLng.Value : null;
            }
        }

        var passenger = await context.DomainUsers
            .FirstOrDefaultAsync(u => u.Id == trip.PassengerId, ct);
        var acceptedAdminName = trip.AcceptedByAdminId.HasValue
            ? await context.AdminProfiles
                .Where(a => a.Id == trip.AcceptedByAdminId.Value)
                .Select(a => a.Name)
                .FirstOrDefaultAsync(ct)
            : null;

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
            passenger?.Phone,
            AssignedAtUtc: trip.AssignedAtUtc,
            ArrivedAtUtc: trip.ArrivedAtUtc,
            StartedAtUtc: trip.StartedAtUtc,
            CompletedAtUtc: trip.CompletedAtUtc,
            EncodedOverviewPolyline: tripRoute?.EncodedPolyline,
            RouteSegments: TripRouteSegmentMapper.FromJson(tripRoute?.SegmentsJson),
            PassengerNote: trip.PassengerNote,
            PassengerRating: trip.PassengerRating,
            RatingComment: trip.RatingComment,
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
            AttentionState: trip.GetAttentionState(now).ToString(),
            Refund: latestRefund?.ToRefundDto());
    }
}
