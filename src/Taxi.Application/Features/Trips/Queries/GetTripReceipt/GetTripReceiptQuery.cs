using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetTripReceipt;

public record GetTripReceiptQuery(Guid TripId) : ICachedQuery<Result<TripReceiptDto>>
{
    public string CacheKey => $"trip-receipt-{TripId}";

    public string[] Tags => [$"trip-financials-{TripId}"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(30);
}
