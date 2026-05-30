using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Contracts.Notifications;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class TripRequestedEventHandler(
    ITripNotifier notifier,
    INotificationService notificationService)
    : INotificationHandler<TripRequested>
{
    private readonly ITripNotifier _notifier = notifier;
    private readonly INotificationService _notificationService = notificationService;

    public async Task Handle(TripRequested notification, CancellationToken ct)
    {
        await _notifier.NotifyTripRequestedAsync(
            notification.TripId,
            notification.VehicleTypeId,
            notification.PassengerId,
            ct);

        // Admins are notified via the shared "admins" FCM topic. Localization
        // keys are resolved inside the notification service (default culture).
        await _notificationService.SendPushNotificationToTopicAsync(
            NotificationTopics.Admins,
            LocalizationKeys.Notification.TripRequestedTitle,
            LocalizationKeys.Notification.TripRequestedBody,
            new Dictionary<string, string>
            {
                { "tripId", notification.TripId.ToString() },
                { "status", "TripRequested" }
            },
            ct);

        // Scheduled-trip activation: also tell the passenger their pre-booked
        // trip just went live and is now searching for a driver.
        if (notification.WasScheduled)
        {
            await _notificationService.SendPushNotificationAsync(
                notification.PassengerId,
                LocalizationKeys.Notification.TripScheduledActivatedTitle,
                LocalizationKeys.Notification.TripScheduledActivatedBody,
                new Dictionary<string, string>
                {
                    { "tripId", notification.TripId.ToString() },
                    { "status", "ScheduledTripActivated" },
                    { "sound", "default" }
                },
                ct);
        }
    }
}
