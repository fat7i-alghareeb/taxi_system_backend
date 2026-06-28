using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetPassengerTrips;

public class GetPassengerTripsQueryHandler(
    ILogger<GetPassengerTripsQueryHandler> logger,
    IAppDbContext context,
    IUser currentUser) : IRequestHandler<GetPassengerTripsQuery, Result<PagedResult<TripSummaryDto>>>
{
    public async Task<Result<PagedResult<TripSummaryDto>>> Handle(GetPassengerTripsQuery request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        logger.LogInformation(
            "[Projection] {QueryName} — PassengerId='{PassengerId}'. Paging={Page}/{PageSize}.",
            nameof(GetPassengerTripsQuery), passengerId, request.Page, request.PageSize);

        var query = context.Trips
            .AsNoTracking()
            .Where(t => t.PassengerId == passengerId && t.Status != TripStatus.AwaitingPayment);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(t => t.ReferenceCode.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Join(context.PricingQuotes, t => t.QuoteId, q => q.Id, (t, q) => new { t, q })
            .Select(x => new TripSummaryDto(
                x.t.Id,
                x.t.ReferenceCode,
                x.t.Status.ToString(),
                x.q.FinalFare,
                x.q.CurrencyCode,
                x.t.CreatedAtUtc,
                x.t.ScheduledAtUtc,
                x.t.Stops
                    .OrderBy(s => s.Sequence)
                    .Select(s => new TripStopDto(
                        s.Coordinate.Latitude,
                        s.Coordinate.Longitude,
                        s.AddressLabel,
                        s.Sequence,
                        s.IsCompleted,
                        s.CompletedAtUtc))
                    .ToList(),
                x.t.PassengerNote))
            .ToListAsync(ct);

        return new PagedResult<TripSummaryDto>(items, totalCount, request.Page, request.PageSize);
    }
}

