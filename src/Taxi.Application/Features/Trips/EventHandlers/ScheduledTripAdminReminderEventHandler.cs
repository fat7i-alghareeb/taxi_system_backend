using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Contracts.Notifications;
using Taxi.Domain.Trips;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

public sealed class ScheduledTripAdminReminderEventHandler(
    INotificationService notificationService)
    : INotificationHandler<ScheduledTripAdminReminder>
{
    public Task Handle(ScheduledTripAdminReminder notification, CancellationToken ct)
    {
        var (title, body) = notification.Stage switch
        {
            ScheduledTripReminderStage.Unaccepted60Minutes =>
                (LocalizationKeys.Notification.AdminTripReminder60Title,
                 LocalizationKeys.Notification.AdminTripReminder60Body),
            ScheduledTripReminderStage.Unaccepted30Minutes =>
                (LocalizationKeys.Notification.AdminTripReminder30Title,
                 LocalizationKeys.Notification.AdminTripReminder30Body),
            ScheduledTripReminderStage.Unaccepted15Minutes =>
                (LocalizationKeys.Notification.AdminTripReminder15Title,
                 LocalizationKeys.Notification.AdminTripReminder15Body),
            ScheduledTripReminderStage.UnacceptedOverdue =>
                (LocalizationKeys.Notification.AdminTripOverdueTitle,
                 LocalizationKeys.Notification.AdminTripOverdueBody),
            ScheduledTripReminderStage.Accepted30Minutes =>
                (LocalizationKeys.Notification.AdminAcceptedReminder30Title,
                 LocalizationKeys.Notification.AdminAcceptedReminder30Body),
            ScheduledTripReminderStage.Accepted15Minutes =>
                (LocalizationKeys.Notification.AdminAcceptedReminder15Title,
                 LocalizationKeys.Notification.AdminAcceptedReminder15Body),
            // Customer-facing stages raise ScheduledTripCustomerReminder instead and
            // never reach here; ignore rather than throw so a future stage cannot
            // take down the reminder dispatcher.
            _ => (Title: null!, Body: null!),
        };

        if (title is null)
        {
            return Task.CompletedTask;
        }

        return notificationService.SendPushNotificationToAdminsAsync(
            title,
            body,
            new Dictionary<string, string>
            {
                { "tripId", notification.TripId.ToString() },
                { "type", "scheduled_trip_admin_reminder" },
                { "stage", notification.Stage.ToString() },
                { "scheduledAtUtc", notification.ScheduledAtUtc.ToString("O") },
                { "eventId", notification.EventId.ToString() },
            },
            ct,
            bodyArgs:
            [
                notification.ReferenceCode,
                notification.ScheduledAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'"),
            ]);
    }
}
