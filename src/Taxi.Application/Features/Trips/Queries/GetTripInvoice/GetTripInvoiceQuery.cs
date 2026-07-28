using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetTripInvoice;

public record GetTripInvoiceQuery(Guid TripId) : ICachedQuery<Result<TripInvoiceDto>>
{
    public string CacheKey => $"trip-invoice-{TripId}";

    public string[] Tags => [$"trip-financials-{TripId}"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(30);
}
