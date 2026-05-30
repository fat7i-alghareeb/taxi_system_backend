using System.Text.Json;

namespace Taxi.Application.Features.Trips.Dtos;

internal static class TripRouteSegmentMapper
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static List<TripRouteSegmentDto>? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var raw = JsonSerializer.Deserialize<List<RawSegment>>(json, Options);
        if (raw is null || raw.Count == 0)
        {
            return null;
        }

        return raw.Select(s => new TripRouteSegmentDto(
            s.DistanceMeters,
            s.DurationSeconds,
            s.EncodedPolyline ?? string.Empty,
            s.StartLatitude,
            s.StartLongitude,
            s.EndLatitude,
            s.EndLongitude)).ToList();
    }

    private sealed class RawSegment
    {
        public int DistanceMeters { get; set; }
        public int DurationSeconds { get; set; }
        public string? EncodedPolyline { get; set; }
        public decimal StartLatitude { get; set; }
        public decimal StartLongitude { get; set; }
        public decimal EndLatitude { get; set; }
        public decimal EndLongitude { get; set; }
    }
}
