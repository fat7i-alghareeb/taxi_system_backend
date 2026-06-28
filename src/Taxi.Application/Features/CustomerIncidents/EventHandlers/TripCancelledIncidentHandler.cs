using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.CustomerIncidents;
using Taxi.Domain.Trips;
using Taxi.Domain.Trips.Events;

namespace Taxi.Application.Features.CustomerIncidents.EventHandlers;

/// <summary>
/// Records a cancellation incident, attributing it to the passenger or the driver and
/// carrying the reason. Admin-initiated cancellations are intentionally NOT recorded —
/// the admin already knows, and they are not a customer problem.
/// </summary>
public sealed class TripCancelledIncidentHandler(IAppDbContext context, ICustomerIncidentRecorder recorder)
    : INotificationHandler<TripCancelled>
{
    public async Task Handle(TripCancelled notification, CancellationToken ct)
    {
        var cancellation = await context.TripCancellations
            .AsNoTracking()
            .Where(c => c.TripId == notification.TripId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        // Skip admin-initiated cancellations (not a customer-facing incident).
        if (cancellation?.Actor == CancellationActor.Admin)
        {
            return;
        }

        // Determine attribution: prefer the recorded actor, else fall back to whether a
        // driver was assigned at cancel time.
        var byDriver = cancellation is not null
            ? cancellation.Actor == CancellationActor.Driver
            : notification.DriverId.HasValue;

        var (type, severity, title) = byDriver
            ? (CustomerIncidentType.TripCancelledByDriver, CustomerIncidentSeverity.Warning, "Driver cancelled the trip")
            : (CustomerIncidentType.TripCancelledByPassenger, CustomerIncidentSeverity.Info, "Passenger cancelled the trip");

        var reason = cancellation is null
            ? null
            : string.IsNullOrWhiteSpace(cancellation.Note)
                ? cancellation.Reason.ToString()
                : $"{cancellation.Reason}: {cancellation.Note}";

        await recorder.RecordAsync(
            type,
            severity,
            notification.PassengerId,
            title,
            sourceEventId: notification.EventId,
            tripId: notification.TripId,
            reason: reason,
            amount: cancellation?.RefundAmount,
            currencyCode: cancellation?.CurrencyCode,
            ct: ct);
    }
}
