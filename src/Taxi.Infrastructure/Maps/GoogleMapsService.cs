using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Taxi.Application.Common.Interfaces;
using Taxi.Infrastructure.Settings;

namespace Taxi.Infrastructure.Maps;

public class GoogleMapsService(
    HttpClient httpClient,
    IOptions<AppSettings> options,
    ILogger<GoogleMapsService> logger) : IDirectionsService
{
    // ── Internal JSON deserialization models ─────────────────────────────────

    private sealed class DirectionsApiResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; init; } = default!;

        [JsonPropertyName("routes")]
        public List<Route> Routes { get; init; } = [];
    }

    private sealed class Route
    {
        [JsonPropertyName("overview_polyline")]
        public OverviewPolyline OverviewPolyline { get; init; } = default!;

        [JsonPropertyName("legs")]
        public List<Leg> Legs { get; init; } = [];
    }

    private sealed class OverviewPolyline
    {
        [JsonPropertyName("points")]
        public string Points { get; init; } = string.Empty;
    }

    private sealed class Leg
    {
        [JsonPropertyName("distance")]
        public MeasureValue Distance { get; init; } = default!;

        [JsonPropertyName("duration")]
        public MeasureValue Duration { get; init; } = default!;
    }

    private sealed class MeasureValue
    {
        [JsonPropertyName("value")]
        public int Value { get; init; }
    }

    // ── Public interface method ──────────────────────────────────────────────

    public async Task<DirectionResponse> GetDirectionsAsync(
        decimal originLat,
        decimal originLng,
        decimal destinationLat,
        decimal destinationLng)
    {
        var apiKey = options.Value.GoogleMapsApiKey;

        // Invariant culture prevents locale-specific decimal separators (e.g. "48,12" vs "48.12")
        var ic = System.Globalization.CultureInfo.InvariantCulture;
        var origin = $"{originLat.ToString(ic)},{originLng.ToString(ic)}";
        var destination = $"{destinationLat.ToString(ic)},{destinationLng.ToString(ic)}";

        var requestUri = $"maps/api/directions/json?origin={origin}&destination={destination}&key={apiKey}";

        logger.LogInformation(
            "Calling Google Directions API: {Origin} → {Destination}",
            origin, destination);

        var httpResponse = await httpClient.GetAsync(requestUri);
        httpResponse.EnsureSuccessStatusCode();

        var apiResult = await httpResponse.Content.ReadFromJsonAsync<DirectionsApiResponse>();

        if (apiResult is null)
        {
            logger.LogError("Google Directions API returned an unparseable body.");
            throw new InvalidOperationException("Google Directions API returned an unparseable response.");
        }

        if (apiResult.Status != "OK")
        {
            logger.LogError("Google Directions API error status: {Status}", apiResult.Status);
            throw new InvalidOperationException($"Google Directions API error: {apiResult.Status}");
        }

        if (apiResult.Routes.Count == 0)
        {
            logger.LogWarning("Google Directions API returned OK but zero routes.");
            throw new InvalidOperationException("Google Directions API returned no routes for the given coordinates.");
        }

        var route = apiResult.Routes[0];
        var leg = route.Legs[0];

        logger.LogInformation(
            "Directions resolved: {DistanceMeters}m, {DurationSeconds}s",
            leg.Distance.Value, leg.Duration.Value);

        return new DirectionResponse(
            DistanceMeters: leg.Distance.Value,
            DurationSeconds: leg.Duration.Value,
            EncodedPolyline: route.OverviewPolyline.Points);
    }
}
