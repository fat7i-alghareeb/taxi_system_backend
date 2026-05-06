namespace Taxi.Application.Common.Interfaces;

public interface IDirectionsService
{
    Task<DirectionResponse> GetDirectionsAsync(decimal originLat, decimal originLng, decimal destinationLat, decimal destinationLng);
}

public record DirectionResponse(
    int DistanceMeters,
    int DurationSeconds,
    string EncodedPolyline);
