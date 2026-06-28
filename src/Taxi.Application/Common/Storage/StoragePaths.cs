using Taxi.Domain.Drivers;

namespace Taxi.Application.Common.Storage;

/// <summary>
/// Single source of truth for where uploaded files live under <c>wwwroot</c>.
///
/// Folder creation and permissions are handled centrally and folder-agnostically
/// (<c>StorageInitializer</c> creates every area in <see cref="AllAreas"/> at startup;
/// the Docker image / prod init service guarantee write access), so adding a new
/// feature folder only requires: add a constant, add it to <see cref="AllAreas"/>,
/// and add a path builder here — no Docker or infrastructure change.
/// </summary>
public static class StoragePaths
{
    public const string Photos = "photos";
    public const string Chat = "chat";
    public const string Compensation = "compensation";
    public const string Recordings = "recordings";
    public const string Documents = "documents";

    /// <summary>
    /// Top-level area roots the storage initializer pre-creates and write-probes at startup.
    /// </summary>
    public static IReadOnlyList<string> AllAreas { get; } =
        [Photos, Chat, Compensation, Recordings, Documents];

    /// <summary>Profile photo — one per user, overwritten on change.</summary>
    public static string ProfilePhoto(Guid userId, string extension) =>
        $"{Photos}/{userId}{extension}";

    /// <summary>In-trip chat photo — unique name under the trip's folder.</summary>
    public static string ChatPhoto(Guid tripId, string extension) =>
        $"{Chat}/trips/{tripId:N}/{Guid.NewGuid():N}{extension}";

    /// <summary>Compensation evidence image — unique name.</summary>
    public static string CompensationEvidence(string extension) =>
        $"{Compensation}/{Guid.NewGuid():N}{extension}";

    /// <summary>Trip safety recording — unique name under the trip's folder.</summary>
    public static string TripRecording(Guid tripId, string extension) =>
        $"{Recordings}/{tripId:N}/{Guid.NewGuid():N}{extension}";

    /// <summary>Driver document — one per driver per document type, overwritten on re-upload.</summary>
    public static string DriverDocument(Guid driverId, DocumentType type, string extension) =>
        $"{Documents}/{driverId}_{type}{extension}";
}
