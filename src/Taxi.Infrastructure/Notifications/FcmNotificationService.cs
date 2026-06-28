using FirebaseAdmin.Messaging;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;

namespace Taxi.Infrastructure.Notifications;

public class FcmNotificationService(
    IAppDbContext context,
    ILogger<FcmNotificationService> logger,
    IStringLocalizerFactory localizerFactory) : INotificationService
{
    private const string DefaultAndroidChannelId = "high_importance";
    private const string DefaultSound = "default";
    private const string DefaultTopicCulture = "en";
    private const string AdminsTopic = "admins";

    private readonly IAppDbContext _context = context;
    private readonly ILogger<FcmNotificationService> _logger = logger;

    public async Task SendPushNotificationAsync(
        Guid userId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default,
        object[]? titleArgs = null,
        object[]? bodyArgs = null)
    {
        _logger.LogInformation(
            "[FCM] SendPushNotification called. UserId={UserId} TitleKey={TitleKey} BodyKey={BodyKey} DataKeys=[{DataKeys}]",
            userId, title, body, data != null ? string.Join(",", data.Keys) : "none");

        var user = await _context.DomainUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
        {
            _logger.LogWarning("[FCM] User {UserId} not found in database. Cannot send push notification.", userId);
            return;
        }

        _logger.LogInformation(
            "[FCM] User found. UserId={UserId} PreferredLanguage={Lang} HasFcmToken={HasToken}",
            userId, user.PreferredLanguage ?? "null", !string.IsNullOrWhiteSpace(user.FcmToken));

        if (string.IsNullOrWhiteSpace(user.FcmToken))
        {
            _logger.LogWarning(
                "[FCM] User {UserId} has no FCM token stored. Skipping. " +
                "(App must call /auth/fcm-token after login to register a token.)",
                userId);
            return;
        }

        var lang = string.IsNullOrWhiteSpace(user.PreferredLanguage) ? "en" : user.PreferredLanguage;
        var (localizedTitle, localizedBody) = Localize(title, body, lang, titleArgs, bodyArgs);

        _logger.LogInformation(
            "[FCM] Sending to user {UserId}. Lang={Lang} Title=\"{Title}\" Body=\"{Body}\" Data={Data}",
            userId, lang, localizedTitle, localizedBody,
            data != null ? string.Join(", ", data.Select(kv => $"{kv.Key}={kv.Value}")) : "none");

        try
        {
            var message = new Message
            {
                Token = user.FcmToken,
                Notification = new Notification
                {
                    Title = localizedTitle,
                    Body = localizedBody,
                },
                Data = data,
                Android = CreateAndroidConfig(),
                Apns = CreateApnsConfig(localizedTitle, localizedBody),
            };

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
            _logger.LogInformation(
                "[FCM] SUCCESS. UserId={UserId} Response={Response}",
                userId, response);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "[FCM] FirebaseMessagingException for user {UserId}. ErrorCode={ErrorCode} HttpCode={HttpCode}",
                userId, ex.MessagingErrorCode, ex.HttpResponse?.StatusCode);
            if (IsInvalidUserToken(ex))
            {
                _logger.LogWarning("[FCM] Token is invalid/unregistered for user {UserId}. Clearing token.", userId);
                await ClearInvalidTokenAsync(user, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FCM] Unexpected error sending push to user {UserId}.", userId);
        }
    }

    public async Task SendPushNotificationToTopicAsync(
        string topic,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default,
        object[]? titleArgs = null,
        object[]? bodyArgs = null)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            _logger.LogWarning("[FCM] Cannot send push notification to topic: topic is empty.");
            return;
        }

        _logger.LogInformation(
            "[FCM] SendPushNotificationToTopic called. Topic={Topic} TitleKey={TitleKey} BodyKey={BodyKey} DataKeys=[{DataKeys}]",
            topic, title, body, data != null ? string.Join(",", data.Keys) : "none");

        // Topic broadcasts have no per-recipient language, so resolve any
        // localization keys in a single default culture. Free-text titles/bodies
        // (e.g. admin broadcasts) are left untouched when no resource matches.
        var (localizedTitle, localizedBody) = Localize(
            title,
            body,
            DefaultTopicCulture,
            titleArgs,
            bodyArgs);

        _logger.LogInformation(
            "[FCM] Sending to topic={Topic} Title=\"{Title}\" Body=\"{Body}\" Data={Data}",
            topic, localizedTitle, localizedBody,
            data != null ? string.Join(", ", data.Select(kv => $"{kv.Key}={kv.Value}")) : "none");

        try
        {
            var message = new Message
            {
                Topic = topic,
                Notification = new Notification
                {
                    Title = localizedTitle,
                    Body = localizedBody,
                },
                Data = data,
                Android = CreateAndroidConfig(),
                Apns = CreateApnsConfig(localizedTitle, localizedBody),
            };

            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
            _logger.LogInformation("[FCM] SUCCESS to topic={Topic}. Response={Response}", topic, response);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "[FCM] FirebaseMessagingException for topic={Topic}. ErrorCode={ErrorCode} HttpCode={HttpCode}",
                topic, ex.MessagingErrorCode, ex.HttpResponse?.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FCM] Unexpected error sending push to topic={Topic}.", topic);
        }
    }

    public async Task SendPushNotificationToAdminsAsync(
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default,
        object[]? titleArgs = null,
        object[]? bodyArgs = null)
    {
        var admins = await _context.AdminProfiles
            .AsNoTracking()
            .Where(admin => admin.IsActive && admin.FcmToken != null)
            .Select(admin => new { admin.Id, admin.FcmToken, admin.PreferredLanguage })
            .ToListAsync(ct);

        _logger.LogInformation(
            "[FCM] SendPushNotificationToAdmins called. TitleKey={TitleKey} BodyKey={BodyKey} ActiveAdminsWithToken={Count} DataKeys=[{DataKeys}]",
            title, body, admins.Count, data != null ? string.Join(",", data.Keys) : "none");

        var deliveredCount = 0;
        foreach (var admin in admins)
        {
            // Localize per-admin so each recipient gets their own preferred language.
            var lang = string.IsNullOrWhiteSpace(admin.PreferredLanguage)
                ? DefaultTopicCulture
                : admin.PreferredLanguage;
            var (localizedTitle, localizedBody) = Localize(title, body, lang, titleArgs, bodyArgs);

            try
            {
                var message = new Message
                {
                    Token = admin.FcmToken,
                    Notification = new Notification
                    {
                        Title = localizedTitle,
                        Body = localizedBody,
                    },
                    Data = data,
                    Android = CreateAndroidConfig(),
                    Apns = CreateApnsConfig(localizedTitle, localizedBody),
                };

                await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
                deliveredCount++;
            }
            catch (FirebaseMessagingException ex) when (IsInvalidUserToken(ex))
            {
                _logger.LogWarning(
                    ex,
                    "[FCM] Clearing invalid admin token for admin {AdminId}.",
                    admin.Id);
                var trackedAdmin = await _context.AdminProfiles
                    .FirstOrDefaultAsync(profile => profile.Id == admin.Id, ct);
                if (trackedAdmin is not null)
                {
                    trackedAdmin.UpdateFcmToken(null);
                    await _context.SaveChangesAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FCM] Failed to send admin push to admin {AdminId}.", admin.Id);
            }
        }

        // Backup channel: if no per-device token delivery succeeded (e.g. all tokens
        // stale/unregistered), fall back to the 'admins' topic so the alert still
        // reaches any admin device subscribed to it. Topic broadcasts can't be
        // per-admin-language, so resolve in the default culture.
        if (deliveredCount == 0)
        {
            _logger.LogWarning(
                "[FCM] No admin device tokens delivered ({Count} candidates). Falling back to '{Topic}' topic.",
                admins.Count, AdminsTopic);
            await SendPushNotificationToTopicAsync(AdminsTopic, title, body, data, ct, titleArgs, bodyArgs);
        }
    }

    private (string Title, string Body) Localize(
        string title,
        string body,
        string lang,
        object[]? titleArgs,
        object[]? bodyArgs)
    {
        var localizer = localizerFactory.Create("Taxi.Api.SharedResource", "Taxi.Api");
        var originalCulture = System.Globalization.CultureInfo.CurrentUICulture;

        var localizedTitle = title;
        var localizedBody = body;

        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo(lang);
            var titleLoc = titleArgs is { Length: > 0 }
                ? localizer[title, titleArgs]
                : localizer[title];
            var bodyLoc = bodyArgs is { Length: > 0 }
                ? localizer[body, bodyArgs]
                : localizer[body];

            if (!titleLoc.ResourceNotFound)
            {
                localizedTitle = titleLoc.Value;
            }

            if (!bodyLoc.ResourceNotFound)
            {
                localizedBody = bodyLoc.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve localization for language '{Lang}' with keys: '{Title}', '{Body}'", lang, title, body);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = originalCulture;
        }

        return (localizedTitle, localizedBody);
    }

    private static AndroidConfig CreateAndroidConfig()
    {
        return new AndroidConfig
        {
            Priority = Priority.High,
            Notification = new AndroidNotification
            {
                ChannelId = DefaultAndroidChannelId,
                Sound = DefaultSound,
            },
        };
    }

    private static ApnsConfig CreateApnsConfig(string title, string body)
    {
        return new ApnsConfig
        {
            Headers = new Dictionary<string, string>
            {
                { "apns-priority", "10" },
            },
            Aps = new Aps
            {
                Alert = new ApsAlert
                {
                    Title = title,
                    Body = body,
                },
                Sound = DefaultSound,
            },
        };
    }

    private static bool IsInvalidUserToken(FirebaseMessagingException ex)
    {
        return ex.MessagingErrorCode is MessagingErrorCode.Unregistered
            or MessagingErrorCode.InvalidArgument;
    }

    private async Task ClearInvalidTokenAsync(Taxi.Domain.Users.User user, CancellationToken ct)
    {
        var updateResult = user.UpdateFcmToken(null);
        if (updateResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to clear invalid FCM token for user {UserId}: {Error}",
                user.Id,
                updateResult.Error);
            return;
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Cleared invalid FCM token for user {UserId}.", user.Id);
    }
}
