namespace Taxi.Domain.Payments;

public sealed record PaymentRefundTotals(
    decimal SuccessfulAmount,
    decimal ReservedAmount,
    decimal FailedAmount,
    decimal AvailableAmount,
    bool IsFullyRefunded);
