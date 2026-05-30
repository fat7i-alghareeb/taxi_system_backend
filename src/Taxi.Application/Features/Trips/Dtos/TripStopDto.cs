namespace Taxi.Application.Features.Trips.Dtos;

public record TripStopDto(
    decimal Latitude,
    decimal Longitude,
    string? Label = null,
    int Sequence = 0,
    bool IsCompleted = false,
    DateTimeOffset? CompletedAtUtc = null);

