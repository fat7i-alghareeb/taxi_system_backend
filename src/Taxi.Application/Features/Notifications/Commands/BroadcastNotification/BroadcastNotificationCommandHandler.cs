using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Contracts.Notifications;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Notifications.Commands.BroadcastNotification;

public class BroadcastNotificationCommandHandler(INotificationService notificationService)
    : IRequestHandler<BroadcastNotificationCommand, Result<Success>>
{
    private readonly INotificationService _notificationService = notificationService;

    public async Task<Result<Success>> Handle(BroadcastNotificationCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Notification.TitleRequired, "Notification title is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Notification.BodyRequired, "Notification body is required."));
        }

        // Implicitly map strongly-typed client inputs to secure FCM topics.
        // The client only sends 0 (Customers), 1 (Drivers), or 2 (Admins) which
        // prevents any arbitrary, unsecured topic names from being passed.
        string topic = request.Audience switch
        {
            NotificationAudience.Customers => NotificationTopics.Customers,
            NotificationAudience.Drivers => NotificationTopics.Drivers,
            NotificationAudience.Admins => NotificationTopics.Admins,
            _ => throw new ArgumentOutOfRangeException(nameof(request.Audience), "Invalid target audience group.")
        };

        await _notificationService.SendPushNotificationToTopicAsync(
            topic,
            request.Title,
            request.Body,
            request.Data,
            ct);

        return Result.Success;
    }
}
