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
}
