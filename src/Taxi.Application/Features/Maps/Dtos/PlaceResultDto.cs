namespace Taxi.Application.Features.Maps.Dtos;

public record PlaceResultDto(
    string PlaceId,
    string PrimaryName,
    string SecondaryAddress,
    decimal Latitude,
    decimal Longitude);
