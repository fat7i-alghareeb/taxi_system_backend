namespace Taxi.Application.Features.Trips.Dtos;

/// <summary>
/// Outcome of applying a mid-trip edit. <see cref="Status"/> is <c>applied</c> when the
/// change is committed (delta==0, a refund, or a fully-settled charge) — <see cref="Trip"/>
/// carries the updated trip. It is <c>requiresPaymentSheet</c> when a fare increase could not
/// be charged silently: the trip is UNCHANGED and the client must present <see cref="PaymentSheet"/>;
/// the held edit is applied by the success webhook (or reverted on failure / expiry).
/// </summary>
public sealed record TripEditApplyResultDto(
    string Status,
    TripDto? Trip,
    decimal Delta,
    string Currency,
    Guid? PendingEditId,
    StripePaymentDto? PaymentSheet)
{
    public const string StatusApplied = "applied";
    public const string StatusRequiresPaymentSheet = "requiresPaymentSheet";

    public static TripEditApplyResultDto Applied(TripDto trip, decimal delta, string currency) =>
        new(StatusApplied, trip, delta, currency, null, null);

    public static TripEditApplyResultDto RequiresSheet(
        decimal delta, string currency, Guid pendingEditId, StripePaymentDto sheet) =>
        new(StatusRequiresPaymentSheet, null, delta, currency, pendingEditId, sheet);
}
