namespace Taxi.Application.Features.RefundIssues.Dtos;

public record RefundIssueDto(
    Guid Id,
    Guid PassengerId,
    Guid TripId,
    Guid? PaymentId,
    Guid? PaymentRefundId,
    Guid? TripCancellationId,
    string RequestType,
    string CustomerReason,
    string? Note,
    string? RefundStatusSnapshot,
    decimal? RefundAmountSnapshot,
    string? RefundCurrencySnapshot,
    string ReviewStatus,
    DateTimeOffset CreatedAtUtc,
    Guid? ReviewedByAdminId,
    DateTimeOffset? ReviewedAtUtc,
    string? AdminNotes,
    bool WhatsAppOpened,
    string? PassengerName,
    string? TripReferenceCode);
