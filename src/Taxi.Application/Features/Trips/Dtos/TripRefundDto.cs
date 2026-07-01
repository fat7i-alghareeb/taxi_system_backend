using Taxi.Domain.Payments;

namespace Taxi.Application.Features.Trips.Dtos;

/// <summary>
/// Customer-safe refund state for a trip, derived from the latest
/// <see cref="PaymentRefund"/>. Collapses the internal 8-state
/// <see cref="PaymentRefundStatus"/> into a few states the passenger app can
/// show clearly. Never exposes failure reasons.
/// </summary>
public record TripRefundDto(
    string Status,
    decimal Amount,
    string CurrencyCode,
    DateTimeOffset? CompletedAtUtc);

public static class TripRefundDtoMapper
{
    /// <summary>Customer-facing status values (kept stable for the client).</summary>
    public const string Completed = "Completed";
    public const string Processing = "Processing";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";

    public static TripRefundDto ToRefundDto(this PaymentRefund refund) =>
        new(
            ToCustomerStatus(refund.Status),
            refund.Amount,
            refund.Currency,
            refund.CompletedAtUtc);

    private static string ToCustomerStatus(PaymentRefundStatus status) => status switch
    {
        PaymentRefundStatus.Succeeded => Completed,
        PaymentRefundStatus.Failed or PaymentRefundStatus.PermanentlyFailed => Failed,
        PaymentRefundStatus.Cancelled => Cancelled,
        _ => Processing,
    };
}
