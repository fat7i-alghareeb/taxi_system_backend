namespace Taxi.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendPushNotificationAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
    Task SendPushNotificationToTopicAsync(string topic, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
}
