using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Taxi.Application.Common.Interfaces;

namespace Taxi.Infrastructure.Notifications;

public class FcmNotificationService(
    IAppDbContext context, 
    ILogger<FcmNotificationService> logger,
    IStringLocalizerFactory localizerFactory) : INotificationService
{
    private readonly IAppDbContext _context = context;
    private readonly ILogger<FcmNotificationService> _logger = logger;

    public async Task SendPushNotificationAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        var user = await _context.DomainUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found. Cannot send push notification.", userId);
            return;
        }

        if (string.IsNullOrWhiteSpace(user.FcmToken))
        {
            _logger.LogInformation("User {UserId} does not have an FCM token registered. Skipping push notification.", userId);
            return;
        }

        var lang = string.IsNullOrWhiteSpace(user.PreferredLanguage) ? "en" : user.PreferredLanguage;
        var localizer = localizerFactory.Create("Taxi.Api.SharedResource", "Taxi.Api");

        var originalCulture = System.Globalization.CultureInfo.CurrentUICulture;
        string localizedTitle = title;
        string localizedBody = body;

        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo(lang);
            var titleLoc = localizer[title];
            var bodyLoc = localizer[body];

            if (!titleLoc.ResourceNotFound) localizedTitle = titleLoc.Value;
            if (!bodyLoc.ResourceNotFound) localizedBody = bodyLoc.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve localization for language '{Lang}' with keys: '{Title}', '{Body}'", lang, title, body);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = originalCulture;
        }

        try
        {
            var message = new Message
            {
                Token = user.FcmToken,
                Notification = new Notification
                {
                    Title = localizedTitle,
                    Body = localizedBody
                },
                Data = data
            };

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
            _logger.LogInformation("Push notification successfully sent to user {UserId}. Response: {Response}", userId, response);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Failed to send FCM push notification to user {UserId}.", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending FCM push notification to user {UserId}.", userId);
        }
    }

    public async Task SendPushNotificationToTopicAsync(string topic, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            _logger.LogWarning("Cannot send push notification: topic is empty.");
            return;
        }

        try
        {
            var message = new Message
            {
                Topic = topic,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data
            };

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
            _logger.LogInformation("Push notification successfully sent to topic {Topic}. Response: {Response}", topic, response);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Failed to send FCM push notification to topic {Topic}.", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending FCM push notification to topic {Topic}.", topic);
        }
    }
}
