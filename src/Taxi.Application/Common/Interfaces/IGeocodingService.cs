namespace Taxi.Application.Common.Interfaces;

public interface IGeocodingService
{
    Task<List<PlaceResult>> SearchPlacesAsync(string query, decimal? biasLat, decimal? biasLng);
    Task<PlaceResult?> ReverseGeocodeAsync(decimal latitude, decimal longitude);
}

public record PlaceResult(
    string PlaceId,
    string PrimaryName,
    string SecondaryAddress,
    decimal Latitude,
    decimal Longitude);

