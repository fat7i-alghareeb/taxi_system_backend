namespace Taxi.Application.Features.Maps.Dtos;

public record DirectionsDto(
    int TotalDistanceMeters,
    int TotalDurationSeconds,
    string EncodedPolyline,
    List<LegDto> Legs);

public record LegDto(
    int DistanceMeters,
    int DurationSeconds,
    string EncodedPolyline,
    string StartLabel,
    string EndLabel);

