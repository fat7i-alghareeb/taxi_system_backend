using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.CustomerIncidents;

/// <summary>
/// A durable, admin-triageable record of a customer-facing problem or a notable
/// customer action (payment failed, trip cancelled + why, low rating, refund, …).
/// Auto-created by event handlers from existing domain events; deduplicated by
/// <see cref="SourceEventId"/> because the outbox dispatches at-least-once.
/// </summary>
public sealed class CustomerIncident : AuditableEntity
{
    private CustomerIncident() { } // EF Core

    private CustomerIncident(
        Guid id,
        Guid passengerId,
        Guid? tripId,
        CustomerIncidentType type,
        CustomerIncidentSeverity severity,
        string title,
        string? reason,
        decimal? amount,
        string? currencyCode,
        Guid sourceEventId)
        : base(id)
    {
        PassengerId = passengerId;
        TripId = tripId;
        Type = type;
        Severity = severity;
        Status = CustomerIncidentStatus.Open;
        Title = title;
        Reason = reason;
        Amount = amount;
        CurrencyCode = currencyCode;
        SourceEventId = sourceEventId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid PassengerId { get; private set; }
    public Guid? TripId { get; private set; }
    public CustomerIncidentType Type { get; private set; }
    public CustomerIncidentSeverity Severity { get; private set; }
    public CustomerIncidentStatus Status { get; private set; }

    /// <summary>Short English summary line (e.g. "Driver cancelled the trip").</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Free-text context (cancellation reason, failure reason, "Rated 2★", …).</summary>
    public string? Reason { get; private set; }

    /// <summary>Monetary amount when relevant (refund amount, fare), else null.</summary>
    public decimal? Amount { get; private set; }
    public string? CurrencyCode { get; private set; }

    /// <summary>The originating domain event id; unique to keep auto-logging idempotent.</summary>
    public Guid SourceEventId { get; private set; }

    /// <summary>Appended admin internal notes (newest last).</summary>
    public string? Notes { get; private set; }

    public Guid? ResolvedByAdminId { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    public static Result<CustomerIncident> Create(
        Guid id,
        Guid passengerId,
        CustomerIncidentType type,
        CustomerIncidentSeverity severity,
        string title,
        Guid sourceEventId,
        Guid? tripId = null,
        string? reason = null,
        decimal? amount = null,
        string? currencyCode = null)
    {
        return new CustomerIncident(
            id,
            passengerId,
            tripId,
            type,
            severity,
            string.IsNullOrWhiteSpace(title) ? type.ToString() : title.Trim(),
            string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            amount,
            currencyCode,
            sourceEventId);
    }

    /// <summary>
    /// Moves the incident to a new status. Resolved/Dismissed are terminal and stamp
    /// the acting admin + time; reopening a closed incident is rejected.
    /// </summary>
    public Result<Success> ChangeStatus(CustomerIncidentStatus newStatus, Guid adminId, string? note)
    {
        if (Status is CustomerIncidentStatus.Resolved or CustomerIncidentStatus.Dismissed)
        {
            return CustomerIncidentErrors.AlreadyClosed;
        }

        Status = newStatus;

        if (newStatus is CustomerIncidentStatus.Resolved or CustomerIncidentStatus.Dismissed)
        {
            ResolvedByAdminId = adminId;
            ResolvedAtUtc = DateTimeOffset.UtcNow;
        }

        AppendNote(adminId, note);
        return Result.Success;
    }

    /// <summary>Appends an internal admin note without changing status.</summary>
    public Result<Success> AddNote(Guid adminId, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return Result.Success;
        }

        AppendNote(adminId, note);
        return Result.Success;
    }

    private void AppendNote(Guid adminId, string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return;
        }

        var stamped = $"[{DateTimeOffset.UtcNow:u}] {adminId}: {note.Trim()}";
        Notes = string.IsNullOrWhiteSpace(Notes) ? stamped : $"{Notes}\n{stamped}";
    }
}
