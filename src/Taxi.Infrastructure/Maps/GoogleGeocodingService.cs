using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Taxi.Application.Common.Interfaces;
using Taxi.Infrastructure.Settings;

namespace Taxi.Infrastructure.Maps;

public class GoogleGeocodingService(
    HttpClient httpClient,
    IOptions<AppSettings> options,
    ILogger<GoogleGeocodingService> logger) : IGeocodingService
{
    // ── Internal JSON deserialization models ─────────────────────────────────

    private sealed class PlacesApiResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; init; } = default!;

        [JsonPropertyName("results")]
        public List<PlaceApiResult> Results { get; init; } = [];
    }

    private sealed class GeocodeApiResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; init; } = default!;

        [JsonPropertyName("results")]
        public List<GeocodeResult> Results { get; init; } = [];
    }

    private sealed class PlaceApiResult
    {
        [JsonPropertyName("place_id")]
        public string PlaceId { get; init; } = default!;

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("formatted_address")]
        public string FormattedAddress { get; init; } = string.Empty;

        [JsonPropertyName("geometry")]
        public GeometryResult Geometry { get; init; } = default!;

        [JsonPropertyName("types")]
        public List<string> Types { get; init; } = [];
    }

    private sealed class GeocodeResult
    {
        [JsonPropertyName("place_id")]
        public string PlaceId { get; init; } = default!;

        [JsonPropertyName("formatted_address")]
        public string FormattedAddress { get; init; } = string.Empty;

        [JsonPropertyName("address_components")]
        public List<AddressComponent> AddressComponents { get; init; } = [];

        [JsonPropertyName("geometry")]
        public GeometryResult Geometry { get; init; } = default!;

        [JsonPropertyName("types")]
        public List<string> Types { get; init; } = [];
    }

    private sealed class AddressComponent
    {
        [JsonPropertyName("long_name")]
        public string LongName { get; init; } = string.Empty;

        [JsonPropertyName("types")]
        public List<string> Types { get; init; } = [];
    }

    private sealed class GeometryResult
    {
        [JsonPropertyName("location")]
        public LatLng Location { get; init; } = default!;
    }

    private sealed class LatLng
    {
        [JsonPropertyName("lat")]
        public decimal Lat { get; init; }

        [JsonPropertyName("lng")]
        public decimal Lng { get; init; }
    }

    // ── Public interface methods ─────────────────────────────────────────────

    public async Task<List<PlaceResult>> SearchPlacesAsync(string query, decimal? biasLat, decimal? biasLng)
    {
        var apiKey = options.Value.GoogleMapsApiKey;
        var ic = CultureInfo.InvariantCulture;

        if (!biasLat.HasValue || !biasLng.HasValue)
        {
            throw new ArgumentException("Latitude and Longitude are required for search biasing.");
        }

        var url = $"maps/api/place/textsearch/json?query={Uri.EscapeDataString(query)}&key={apiKey}&location={biasLat.Value.ToString(ic)},{biasLng.Value.ToString(ic)}&radius=15000";

        logger.LogInformation("Calling Google Places Text Search API: {Query} with bias: {Lat},{Lng}", query, biasLat, biasLng);

        var httpResponse = await httpClient.GetAsync(url);
        httpResponse.EnsureSuccessStatusCode();

        var apiResult = await httpResponse.Content.ReadFromJsonAsync<PlacesApiResponse>();

        if (apiResult is null || (apiResult.Status != "OK" && apiResult.Status != "ZERO_RESULTS"))
        {
            logger.LogError("Google Places API error: {Status}", apiResult?.Status);
            return [];
        }

        return apiResult.Results.Take(30).Select(r =>
        {
            var parts = r.FormattedAddress.Split(',', 2);
            var primary = r.Name.Length > 0 ? r.Name : parts[0].Trim();
            var secondary = parts.Length > 1 ? parts[1].Trim() : r.FormattedAddress;

            return new PlaceResult(
                r.PlaceId,
                primary,
                secondary,
                r.Geometry.Location.Lat,
                r.Geometry.Location.Lng,
                r.Types.Contains("airport", StringComparer.OrdinalIgnoreCase));
        }).ToList();
    }

    public async Task<PlaceResult?> ReverseGeocodeAsync(decimal latitude, decimal longitude)
    {
        var apiKey = options.Value.GoogleMapsApiKey;
        var ic = CultureInfo.InvariantCulture;

        var latlng = $"{latitude.ToString(ic)},{longitude.ToString(ic)}";
        var url = $"maps/api/geocode/json?latlng={latlng}&key={apiKey}";

        logger.LogInformation("Calling Google Geocoding API: {LatLng}", latlng);

        var httpResponse = await httpClient.GetAsync(url);
        httpResponse.EnsureSuccessStatusCode();

        var apiResult = await httpResponse.Content.ReadFromJsonAsync<GeocodeApiResponse>();

        if (apiResult is null || apiResult.Status != "OK" || apiResult.Results.Count == 0)
        {
            logger.LogWarning("Google Geocoding API returned no results for {LatLng}", latlng);
            return null;
        }

        var result = apiResult.Results[0];
        var isAirport = apiResult.Results.Any(r =>
            r.Types.Contains("airport", StringComparer.OrdinalIgnoreCase));

        var streetNumber = result.AddressComponents
            .FirstOrDefault(c => c.Types.Contains("street_number"))?.LongName ?? string.Empty;
        var route = result.AddressComponents
            .FirstOrDefault(c => c.Types.Contains("route"))?.LongName ?? string.Empty;
        var city = result.AddressComponents
            .FirstOrDefault(c => c.Types.Contains("locality"))?.LongName ?? string.Empty;

        var primary = route.Length > 0
            ? (streetNumber.Length > 0 ? $"{route} {streetNumber}" : route)
            : result.FormattedAddress.Split(',')[0].Trim();

        // If primary looks like a Plus Code (e.g. "54PH+PM9"), try to find a better name
        if (primary.Contains('+') && !primary.Contains(' '))
        {
            var neighborhood = result.AddressComponents.FirstOrDefault(c => c.Types.Contains("neighborhood"))?.LongName;
            var sublocality = result.AddressComponents.FirstOrDefault(c => c.Types.Contains("sublocality"))?.LongName;
            primary = neighborhood ?? sublocality ?? primary;
        }

        var secondary = city.Length > 0 ? city : result.FormattedAddress;

        // If primary and secondary are same, try to expand secondary
        if (primary == secondary && result.AddressComponents.Count > 0)
        {
            secondary = result.FormattedAddress;
        }

        return new PlaceResult(
            result.PlaceId,
            primary,
            secondary,
            result.Geometry.Location.Lat,
            result.Geometry.Location.Lng,
            isAirport);
    }
}

