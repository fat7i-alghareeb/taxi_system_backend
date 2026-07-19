namespace Taxi.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendPushNotificationAsync(
        Guid userId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default,
        object[]? titleArgs = null,
        object[]? bodyArgs = null);

    Task SendPushNotificationToTopicAsync(
        string topic,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default,
        object[]? titleArgs = null,
        object[]? bodyArgs = null);

    Task SendPushNotificationToAdminsAsync(
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default,
        object[]? titleArgs = null,
        object[]? bodyArgs = null);

    /// <summary>
    /// Sends to a single admin, identified by their <c>AdminProfile.Id</c> (which is the identity
    /// user id — the same value stored in <c>Trip.AcceptedByAdminId</c>). Note that admins live in
    /// <c>AdminProfiles</c>, not <c>DomainUsers</c>, so
    /// <see cref="SendPushNotificationAsync"/> cannot reach them. No-ops when the admin is
    /// inactive or has no device token.
    /// </summary>
    Task SendPushNotificationToAdminAsync(
        Guid adminId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default,
        object[]? titleArgs = null,
        object[]? bodyArgs = null);
}
