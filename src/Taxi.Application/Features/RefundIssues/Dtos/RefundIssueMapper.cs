using Taxi.Domain.RefundIssues;

namespace Taxi.Application.Features.RefundIssues.Dtos;

public static class RefundIssueMapper
{
    public static RefundIssueDto ToDto(
        this RefundIssue issue,
        string? passengerName = null,
        string? tripReferenceCode = null)
        => new(
            issue.Id,
            issue.PassengerId,
            issue.TripId,
            issue.PaymentId,
            issue.PaymentRefundId,
            issue.TripCancellationId,
            issue.RequestType.ToString(),
            issue.CustomerReason,
            issue.Note,
            issue.RefundStatusSnapshot?.ToString(),
            issue.RefundAmountSnapshot,
            issue.RefundCurrencySnapshot,
            issue.ReviewStatus.ToString(),
            issue.CreatedAtUtc,
            issue.ReviewedByAdminId,
            issue.ReviewedAtUtc,
            issue.AdminNotes,
            issue.WhatsAppOpened,
            passengerName,
            tripReferenceCode);
}
