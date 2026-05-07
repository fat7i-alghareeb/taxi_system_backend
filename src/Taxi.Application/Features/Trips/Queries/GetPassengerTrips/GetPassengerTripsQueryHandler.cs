using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetPassengerTrips;

public class GetPassengerTripsQueryHandler(
    IAppDbContext context,
    IUser currentUser) : IRequestHandler<GetPassengerTripsQuery, Result<PagedResult<TripSummaryDto>>>
{
    public async Task<Result<PagedResult<TripSummaryDto>>> Handle(GetPassengerTripsQuery request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var query = context.Trips
            .Where(t => t.PassengerId == passengerId)
            .OrderByDescending(t => t.CreatedAtUtc);

        var totalCount = await query.CountAsync(ct);

        var trips = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var quoteIds = trips.Select(t => t.QuoteId).Distinct().ToList();
        var quotes = await context.PricingQuotes
            .Where(q => quoteIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, ct);

        var items = trips.Select(trip =>
        {
            quotes.TryGetValue(trip.QuoteId, out var quote);
            var stopDtos = trip.Stops
                .OrderBy(s => s.Sequence)
                .Select(s => new TripStopDto(s.Coordinate.Latitude, s.Coordinate.Longitude))
                .ToList();

            return new TripSummaryDto(
                trip.Id,
                trip.ReferenceCode,
                trip.Status.ToString(),
                quote?.FinalFare ?? 0,
                quote?.CurrencyCode ?? "EUR",
                trip.CreatedAtUtc,
                trip.ScheduledAtUtc,
                stopDtos);
        }).ToList();

        return new PagedResult<TripSummaryDto>(items, totalCount, request.Page, request.PageSize);
    }
}
