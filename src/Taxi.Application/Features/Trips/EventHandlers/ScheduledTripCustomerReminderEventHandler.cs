using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Contracts.Notifications;
using Taxi.Domain.Trips;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.Trips.EventHandlers;

/// <summary>
/// Delivers the countdown reminders the passenger is promised on the booking
/// confirmation screen ("you will receive 3 notifications before your ride").
/// The third one — driver arrived — is sent by <c>DriverArrivedEventHandler</c>.
/// </summary>
public sealed class ScheduledTripCustomerReminderEventHandler(
    INotificationService notificationService)
    : INotificationHandler<ScheduledTripCustomerReminder>
{
    public Task Handle(ScheduledTripCustomerReminder notification, CancellationToken ct)
    {
        var keys = notification.Stage switch
        {
            ScheduledTripReminderStage.Customer30Minutes =>
                (Title: LocalizationKeys.Notification.TripCustomerReminder30Title,
                 Body: LocalizationKeys.Notification.TripCustomerReminder30Body),
            ScheduledTripReminderStage.Customer15Minutes =>
                (Title: LocalizationKeys.Notification.TripCustomerReminder15Title,
                 Body: LocalizationKeys.Notification.TripCustomerReminder15Body),
            _ => default,
        };

        if (keys.Title is null)
        {
            return Task.CompletedTask;
        }

        var scheduledLabel = notification.ScheduledAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'");

        return notificationService.SendPushNotificationAsync(
            notification.PassengerId,
            keys.Title,
            keys.Body,
            new Dictionary<string, string>
            {
                { "tripId", notification.TripId.ToString() },
                { "type", "scheduled_trip_customer_reminder" },
                { "stage", notification.Stage.ToString() },
                { "scheduledAtUtc", notification.ScheduledAtUtc.ToString("O") },
                { "eventId", notification.EventId.ToString() },
            },
            ct,
            bodyArgs: [scheduledLabel]);
    }
}
