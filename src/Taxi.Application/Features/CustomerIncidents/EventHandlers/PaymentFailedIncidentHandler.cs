using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.CustomerIncidents;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.CustomerIncidents.EventHandlers;

/// <summary>Records a Critical incident whenever a passenger's payment fails.</summary>
public sealed class PaymentFailedIncidentHandler(ICustomerIncidentRecorder recorder)
    : INotificationHandler<PaymentFailed>
{
    public Task Handle(PaymentFailed notification, CancellationToken ct) =>
        recorder.RecordAsync(
            CustomerIncidentType.PaymentFailed,
            CustomerIncidentSeverity.Critical,
            notification.PassengerId,
            title: "Payment failed",
            sourceEventId: notification.EventId,
            tripId: notification.TripId,
            reason: string.IsNullOrWhiteSpace(notification.Reason) ? null : notification.Reason,
            ct: ct);
}
