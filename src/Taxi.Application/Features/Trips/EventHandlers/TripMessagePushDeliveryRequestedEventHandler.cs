using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Trips;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripMessagePushDeliveryRequestedEventHandler(INotificationService notificationService)
    : INotificationHandler<TripMessagePushDeliveryRequested>
{
    public async Task Handle(TripMessagePushDeliveryRequested notification, CancellationToken ct)
    {
        var body = string.IsNullOrWhiteSpace(notification.Content)
            ? LocalizationKeys.Notification.NewPhotoMessageBody
            : notification.Content;

        var data = new Dictionary<string, string>
        {
            { "type", "chat_message" },
            { "tripId", notification.TripId.ToString() },
            { "messageId", notification.MessageId.ToString() },
        };

        var recipients = new HashSet<Guid> { notification.PassengerId };
        if (notification.DriverUserId is { } driverUserId)
        {
            recipients.Add(driverUserId);
        }

        recipients.Remove(notification.SenderId);
        foreach (var recipient in recipients)
        {
            await notificationService.SendPushNotificationAsync(
                recipient,
                LocalizationKeys.Notification.NewMessageTitle,
                body,
                data,
                ct);
        }

        if (notification.SenderRole == TripMessageSenderRole.Passenger)
        {
            await notificationService.SendPushNotificationToAdminsAsync(
                LocalizationKeys.Notification.NewMessageTitle,
                body,
                data,
                ct);
        }
    }
}
