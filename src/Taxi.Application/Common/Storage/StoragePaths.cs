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

    /// <summary>
    /// Profile photo. The random file name matters: static files are served without
    /// authorization, and the previous <c>photos/{userId}{ext}</c> template let anyone
    /// who learned a user id fetch that user's photo anonymously.
    /// Callers must delete the previous file — the name no longer repeats.
    /// </summary>
    public static string ProfilePhoto(Guid userId, string extension) =>
        $"{Photos}/{userId:N}/{Guid.NewGuid():N}{extension}";

    /// <summary>In-trip chat photo — unique name under the trip's folder.</summary>
    public static string ChatPhoto(Guid tripId, string extension) =>
        $"{Chat}/trips/{tripId:N}/{Guid.NewGuid():N}{extension}";

    /// <summary>Compensation evidence image — unique name.</summary>
    public static string CompensationEvidence(string extension) =>
        $"{Compensation}/{Guid.NewGuid():N}{extension}";

    /// <summary>Trip safety recording — unique name under the trip's folder.</summary>
    public static string TripRecording(Guid tripId, string extension) =>
        $"{Recordings}/{tripId:N}/{Guid.NewGuid():N}{extension}";

    /// <summary>
    /// Driver KYC document. The driver id stays as its own path segment so
    /// <c>ProtectedFilesMiddleware</c> can authorize the download, while the random file
    /// name removes the enumeration primitive the old
    /// <c>documents/{driverId}_{type}{ext}</c> template handed out.
    /// Callers must delete the superseded file — the name no longer repeats.
    /// </summary>
    public static string DriverDocument(Guid driverId, DocumentType type, string extension) =>
        $"{Documents}/{driverId:N}/{Guid.NewGuid():N}{extension}";
}
