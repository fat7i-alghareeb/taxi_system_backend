namespace Taxi.Application.Features.Trips.Dtos;

public record TripSummaryDto(
    Guid Id,
    string ReferenceCode,
    string Status,
    decimal QuotedFare,
    string CurrencyCode,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ScheduledAtUtc,
    List<TripStopDto> Stops);
