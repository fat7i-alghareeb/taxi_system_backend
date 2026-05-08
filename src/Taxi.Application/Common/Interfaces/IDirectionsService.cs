namespace Taxi.Application.Common.Interfaces;

public interface IDirectionsService
{
    Task<DirectionResponse> GetDirectionsAsync(decimal originLat, decimal originLng, decimal destinationLat, decimal destinationLng);
    Task<MultiStopDirectionResponse> GetDirectionsAsync(List<Coordinate> stops);
}

public record Coordinate(decimal Latitude, decimal Longitude);

public record MultiStopDirectionResponse(
    int TotalDistanceMeters,
    int TotalDurationSeconds,
    string OverviewPolyline,
    List<LegDetail> Legs);

public record LegDetail(
    int DistanceMeters,
    int DurationSeconds,
    string EncodedPolyline,
    string StartLabel,
    string EndLabel);

public record DirectionResponse(
    int DistanceMeters,
    int DurationSeconds,
    string EncodedPolyline);

