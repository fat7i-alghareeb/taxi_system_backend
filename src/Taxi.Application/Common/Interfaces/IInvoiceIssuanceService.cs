using Taxi.Domain.Common.Results;
using Taxi.Domain.Invoices;

namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Issues (or returns the previously-issued) <see cref="Invoice"/> for a
/// completed trip. Designed to be called from both the
/// <c>TripCompletedEventHandler</c> (eager, at the moment of completion) and
/// the read-side endpoints (lazy, the first time a customer asks for the
/// receipt / invoice / PDF on a trip that completed before invoicing existed).
/// </summary>
public interface IInvoiceIssuanceService
{
    /// <summary>
    /// If an invoice already exists for <paramref name="tripId"/>, returns it.
    /// Otherwise issues a fresh invoice from the trip + quote + payment
    /// snapshot, persists it, and returns it. Fails when the trip is not
    /// completed (intermediate states have no authoritative amount yet).
    /// </summary>
    Task<Result<Invoice>> EnsureIssuedAsync(Guid tripId, CancellationToken ct);
}
