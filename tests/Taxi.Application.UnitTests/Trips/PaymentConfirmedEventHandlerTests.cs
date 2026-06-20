using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.EventHandlers;
using Taxi.Contracts.Common;
using Taxi.Contracts.Notifications;
using Taxi.Domain.Trips.Events;

using Xunit;

namespace Taxi.Application.UnitTests.Trips;

public class PaymentConfirmedEventHandlerTests
{
    [Fact]
    public async Task Handle_ScheduledTrip_NotifiesEveryAdminChannelAfterPayment()
    {
        var notifier = Substitute.For<ITripNotifier>();
        var notifications = Substitute.For<INotificationService>();
        var handler = new PaymentConfirmedEventHandler(notifier, notifications);
        var scheduledAt = new DateTimeOffset(
            2026,
            6,
            21,
            8,
            30,
            0,
            TimeSpan.Zero);
        var domainEvent = new PaymentConfirmed
        {
            TripId = Guid.NewGuid(),
            PassengerId = Guid.NewGuid(),
            VehicleTypeId = Guid.NewGuid(),
            ReferenceCode = "TRP-ADMIN",
            ScheduledAtUtc = scheduledAt,
        };

        await handler.Handle(domainEvent, CancellationToken.None);

        await notifier.Received(1).NotifyTripAwaitingAdminAcceptanceAsync(
            domainEvent.TripId,
            domainEvent.VehicleTypeId,
            domainEvent.PassengerId,
            scheduledAt,
            Arg.Any<CancellationToken>(),
            domainEvent.EventId);
        await notifications.Received(1).SendPushNotificationToTopicAsync(
            NotificationTopics.Admins,
            LocalizationKeys.Notification.AdminScheduledTripTitle,
            LocalizationKeys.Notification.AdminScheduledTripBody,
            Arg.Is<Dictionary<string, string>>(data =>
                data["tripId"] == domainEvent.TripId.ToString() &&
                data["eventId"] == domainEvent.EventId.ToString()),
            Arg.Any<CancellationToken>(),
            Arg.Is<object[]?>(args => args == null),
            Arg.Is<object[]?>(args => args != null && args.Length == 2));
    }
}
