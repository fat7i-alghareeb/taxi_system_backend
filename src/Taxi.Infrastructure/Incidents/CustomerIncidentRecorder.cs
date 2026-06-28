using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.CustomerIncidents;

namespace Taxi.Infrastructure.Incidents;

/// <summary>
/// Persists customer incidents idempotently and fans out an admin notification.
/// Called from MediatR event handlers that translate existing domain events into
/// incidents. Safe under the at-least-once outbox: duplicate source events are
/// dropped via the unique <see cref="CustomerIncident.SourceEventId"/> index.
/// </summary>
public sealed class CustomerIncidentRecorder(
    IAppDbContext context,
    ITripNotifier notifier,
    INotificationService notificationService,
    ILogger<CustomerIncidentRecorder> logger)
    : ICustomerIncidentRecorder
{
    public async Task RecordAsync(
        CustomerIncidentType type,
        CustomerIncidentSeverity severity,
        Guid passengerId,
        string title,
        Guid sourceEventId,
        Guid? tripId = null,
        string? reason = null,
        decimal? amount = null,
        string? currencyCode = null,
        CancellationToken ct = default)
    {
        // Fast-path dedupe: the same domain event may be dispatched more than once.
        var alreadyRecorded = await context.CustomerIncidents
            .AsNoTracking()
            .AnyAsync(i => i.SourceEventId == sourceEventId, ct);
        if (alreadyRecorded)
        {
            return;
        }

        var incidentResult = CustomerIncident.Create(
            Guid.NewGuid(),
            passengerId,
            type,
            severity,
            title,
            sourceEventId,
            tripId,
            reason,
            amount,
            currencyCode);
        if (incidentResult.IsError)
        {
            logger.LogWarning(
                "[CustomerIncidentRecorder] Skipped incident type={Type} for passenger={PassengerId}: {Error}",
                type,
                passengerId,
                incidentResult.Error.Description);
            return;
        }

        var incident = incidentResult.Value;
        context.CustomerIncidents.Add(incident);

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Lost the race against a concurrent dispatch of the same source event;
            // the unique SourceEventId index rejected the duplicate. That's the
            // idempotency guarantee doing its job — nothing more to do.
            logger.LogInformation(
                ex,
                "[CustomerIncidentRecorder] Duplicate incident for sourceEventId={SourceEventId} ignored.",
                sourceEventId);
            return;
        }

        await notifier.NotifyCustomerIncidentRaisedToAdminsAsync(
            incident.Id,
            passengerId,
            tripId,
            type.ToString(),
            severity.ToString(),
            ct,
            sourceEventId);

        // Critical incidents (e.g. payment failures) also get a push so admins are
        // alerted even when no dashboard is open. Plain text — admins' localizer
        // leaves unknown strings untouched.
        if (severity == CustomerIncidentSeverity.Critical)
        {
            await notificationService.SendPushNotificationToAdminsAsync(
                "New critical customer incident",
                title,
                new Dictionary<string, string>
                {
                    { "type", "customer_incident" },
                    { "incidentId", incident.Id.ToString() },
                    { "incidentType", type.ToString() },
                },
                ct);
        }
    }
}
