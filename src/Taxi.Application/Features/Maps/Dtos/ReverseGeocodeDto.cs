namespace Taxi.Application.Features.Maps.Dtos;

public record ReverseGeocodeDto(
    string PrimaryName,
    string SecondaryAddress,
    decimal Latitude,
    decimal Longitude,
    bool IsAirport);

