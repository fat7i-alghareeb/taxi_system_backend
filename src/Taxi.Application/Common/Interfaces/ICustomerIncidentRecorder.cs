using Taxi.Domain.CustomerIncidents;

namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Central, idempotent entry point for auto-creating customer incidents from domain
/// events. Implementations dedupe on <paramref name="sourceEventId"/> (the originating
/// domain event id) so the at-least-once outbox never produces duplicate incidents,
/// persist the record, and notify admins (live + push for critical).
/// </summary>
public interface ICustomerIncidentRecorder
{
    Task RecordAsync(
        CustomerIncidentType type,
        CustomerIncidentSeverity severity,
        Guid passengerId,
        string title,
        Guid sourceEventId,
        Guid? tripId = null,
        string? reason = null,
        decimal? amount = null,
        string? currencyCode = null,
        CancellationToken ct = default);
}
