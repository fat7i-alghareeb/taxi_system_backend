using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Drivers.Queries.GetDriverEarnings;

public class GetDriverEarningsQueryHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<GetDriverEarningsQuery, Result<DriverEarningsDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<DriverEarningsDto>> Handle(GetDriverEarningsQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var driverUserId))
        {
            return Result.Failure<DriverEarningsDto>(Error.Validation("Identity.Unauthorized", "Unauthorized user."));
        }

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == driverUserId, ct);
        if (driver == null)
        {
            return Result.Failure<DriverEarningsDto>(Error.NotFound("Driver.NotFound", "Driver profile not found."));
        }

        var completedTrips = await _context.Trips
            .Where(t => t.DriverId == driver.Id && t.Status == TripStatus.Completed && t.DeletedAtUtc == null)
            .Join(
                _context.PricingQuotes,
                t => t.QuoteId,
                q => q.Id,
                (t, q) => new DriverTripEarningDto(
                    t.Id,
                    t.ReferenceCode,
                    q.FinalFare,
                    q.CurrencyCode,
                    t.CompletedAtUtc ?? DateTimeOffset.UtcNow))
            .ToListAsync(ct);

        var totalEarnings = completedTrips.Sum(t => t.Fare);
        var totalTrips = completedTrips.Count;
        var currency = completedTrips.FirstOrDefault()?.CurrencyCode ?? "EUR";

        return new DriverEarningsDto(totalTrips, totalEarnings, currency, completedTrips);
    }
}
