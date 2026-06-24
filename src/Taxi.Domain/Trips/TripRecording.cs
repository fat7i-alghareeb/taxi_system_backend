using Taxi.Domain.Common;

namespace Taxi.Domain.Trips;

/// <summary>
/// The kind of media a <see cref="TripRecording"/> holds. Only <see cref="Audio"/>
/// is captured today; <see cref="Video"/> exists so the feature can grow without a
/// schema change.
/// </summary>
public enum RecordingType
{
    Audio,
    Video
}

/// <summary>
/// A passenger-initiated safety recording captured during an active trip and
/// uploaded to our own storage. Not part of the <see cref="Trip"/> aggregate — it
/// carries no trip invariants and is keyed only by <see cref="TripId"/>.
/// </summary>
public sealed class TripRecording : AuditableEntity
{
    private TripRecording() { } // EF Core

    private TripRecording(
        Guid id,
        Guid tripId,
        Guid passengerId,
        RecordingType type,
        string fileUrl,
        long fileSizeBytes,
        int? durationSeconds)
        : base(id)
    {
        TripId = tripId;
        PassengerId = passengerId;
        Type = type;
        FileUrl = fileUrl;
        FileSizeBytes = fileSizeBytes;
        DurationSeconds = durationSeconds;
        RecordedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid TripId { get; private set; }
    public Guid PassengerId { get; private set; }
    public RecordingType Type { get; private set; }
    public string FileUrl { get; private set; } = default!;
    public long FileSizeBytes { get; private set; }
    public int? DurationSeconds { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static TripRecording Create(
        Guid tripId,
        Guid passengerId,
        RecordingType type,
        string fileUrl,
        long fileSizeBytes,
        int? durationSeconds) =>
        new(Guid.NewGuid(), tripId, passengerId, type, fileUrl, fileSizeBytes, durationSeconds);
}
