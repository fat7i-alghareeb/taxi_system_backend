using Taxi.Domain.Common;

namespace Taxi.Domain.Invoices.Events;

public sealed class InvoiceIssued : DomainEvent
{
    public Guid InvoiceId { get; init; }

    public Guid TripId { get; init; }

    public Guid PassengerId { get; init; }

    public string InvoiceNumber { get; init; } = default!;

    public decimal GrossAmount { get; init; }

    public string CurrencyCode { get; init; } = default!;
}
