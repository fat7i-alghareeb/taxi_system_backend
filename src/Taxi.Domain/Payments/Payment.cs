using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Payments;

public sealed class Payment : AuditableEntity
{
    private Payment() { }

    private Payment(
        Guid id,
        Guid tripId,
        decimal amount,
        string currency,
        PaymentMethod method)
        : base(id)
    {
        TripId = tripId;
        Amount = amount;
        Currency = currency;
        Method = method;
        Status = PaymentStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid TripId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? TransactionReference { get; private set; }

    public static Result<Payment> Create(
        Guid id,
        Guid tripId,
        decimal amount,
        string currency,
        PaymentMethod method)
    {
        if (amount <= 0)
        {
            return PaymentErrors.InvalidAmount;
        }

        return new Payment(id, tripId, amount, currency, method);
    }

    public void MarkAsCompleted(string? reference = null)
    {
        Status = PaymentStatus.Completed;
        ProcessedAtUtc = DateTime.UtcNow;
        TransactionReference = reference;
    }

    public void MarkAsFailed()
    {
        Status = PaymentStatus.Failed;
        ProcessedAtUtc = DateTime.UtcNow;
    }
}
