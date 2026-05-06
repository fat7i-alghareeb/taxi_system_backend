using Taxi.Application.Common.Interfaces;

namespace Taxi.Infrastructure.Maps;

public class GoogleMapsService : IDirectionsService
{
    public Task<DirectionResponse> GetDirectionsAsync(decimal originLat, decimal originLng, decimal destinationLat, decimal destinationLng)
    {
        // Mocked response for now
        // In a real implementation, this would call Google Directions API

        return Task.FromResult(new DirectionResponse(
            DistanceMeters: 5000, // 5km
            DurationSeconds: 600)); // 10 minutes
    }
}
