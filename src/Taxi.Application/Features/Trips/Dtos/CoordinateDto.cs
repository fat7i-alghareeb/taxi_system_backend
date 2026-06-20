namespace Taxi.Application.Features.Trips.Dtos;

public record CoordinateDto(
    decimal Latitude,
    decimal Longitude,
    string? Label = null,
    bool IsAirport = false);

