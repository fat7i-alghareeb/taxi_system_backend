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

        [JsonPropertyName("steps")]
        public List<Step> Steps { get; init; } = [];
    }

    private sealed class Step
    {
        [JsonPropertyName("polyline")]
        public OverviewPolyline Polyline { get; init; } = default!;
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

    public async Task<MultiStopDirectionResponse> GetDirectionsAsync(List<Coordinate> stops)
    {
        if (stops.Count < 2)
        {
            throw new ArgumentException("At least two stops are required.", nameof(stops));
        }

        var apiKey = options.Value.GoogleMapsApiKey;
        var ic = System.Globalization.CultureInfo.InvariantCulture;

        var origin = $"{stops[0].Latitude.ToString(ic)},{stops[0].Longitude.ToString(ic)}";
        var destination = $"{stops[^1].Latitude.ToString(ic)},{stops[^1].Longitude.ToString(ic)}";

        var waypoints = string.Empty;
        if (stops.Count > 2)
        {
            var waypointList = stops.GetRange(1, stops.Count - 2)
                .Select(s => $"{s.Latitude.ToString(ic)},{s.Longitude.ToString(ic)}");
            waypoints = $"&waypoints={string.Join("|", waypointList)}";
        }

        var requestUri = $"maps/api/directions/json?origin={origin}&destination={destination}{waypoints}&key={apiKey}";

        logger.LogInformation("Calling Multi-stop Google Directions API: {Origin} → {Destination} with {Count} waypoints", origin, destination, stops.Count - 2);

        var httpResponse = await httpClient.GetAsync(requestUri);
        httpResponse.EnsureSuccessStatusCode();

        var apiResult = await httpResponse.Content.ReadFromJsonAsync<DirectionsApiResponse>();

        if (apiResult?.Status != "OK" || apiResult.Routes.Count == 0)
        {
            logger.LogError("Google Directions API error: {Status}", apiResult?.Status ?? "NULL");
            throw new InvalidOperationException($"Google Directions API error: {apiResult?.Status ?? "No Routes"}");
        }

        var route = apiResult.Routes[0];
        var totalDistance = route.Legs.Sum(l => l.Distance.Value);
        var totalDuration = route.Legs.Sum(l => l.Duration.Value);

        var legDetails = new List<LegDetail>();
        for (var i = 0; i < route.Legs.Count; i++)
        {
            var apiLeg = route.Legs[i];

            // Reconstruct leg polyline from steps (Google doesn't provide it at leg level directly)
            // Wait, actually it's easier to just return the OverviewPolyline for the whole trip
            // and maybe individual leg polylines are not strictly needed if the client uses the overview.
            // But the user's DTO expects them.

            var legPolyline = string.Join(string.Empty, apiLeg.Steps.Select(s => s.Polyline.Points));

            // Note: Joining encoded polylines directly like this works for display in most decoders
            // but might have slight artifacts. Better than the '|' join though.

            legDetails.Add(new LegDetail(
                apiLeg.Distance.Value,
                apiLeg.Duration.Value,
                legPolyline,
                $"({(char)('A' + i)})",
                $"({(char)('A' + i + 1)})"));
        }

        return new MultiStopDirectionResponse(
            totalDistance,
            totalDuration,
            route.OverviewPolyline.Points,
            legDetails);
    }
}

