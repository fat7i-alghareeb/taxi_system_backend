using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Trips;

public sealed class TripRoute : Entity
{
    private TripRoute() { }

    private TripRoute(
        Guid id,
        Guid tripId,
        string encodedPolyline,
        int totalDistanceMeters,
        int totalDurationSeconds,
        string segmentsJson)
        : base(id)
    {
        TripId = tripId;
        EncodedPolyline = encodedPolyline;
        TotalDistanceMeters = totalDistanceMeters;
        TotalDurationSeconds = totalDurationSeconds;
        SegmentsJson = segmentsJson;
        ComputedAtUtc = DateTime.UtcNow;
    }

    public Guid TripId { get; private set; }
    public string EncodedPolyline { get; private set; } = default!;
    public int TotalDistanceMeters { get; private set; }
    public int TotalDurationSeconds { get; private set; }
    public string SegmentsJson { get; private set; } = default!;
    public DateTime ComputedAtUtc { get; private set; }

    public static Result<TripRoute> Create(
        Guid id,
        Guid tripId,
        string encodedPolyline,
        int totalDistanceMeters,
        int totalDurationSeconds,
        string segmentsJson)
    {
        return new TripRoute(
            id,
            tripId,
            encodedPolyline,
            totalDistanceMeters,
            totalDurationSeconds,
            segmentsJson);
    }
}

