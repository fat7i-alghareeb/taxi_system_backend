using Taxi.Domain.RefundIssues;

namespace Taxi.Application.Features.Trips.Dtos;

/// <summary>
/// Customer-safe view of the passenger's latest <see cref="RefundIssue"/> for a trip, so the app can
/// render "your request is under review" instead of the submit form without a second network call
/// (the refund-issue read endpoints are admin-only).
///
/// Deliberately omits Note, AdminNotes, ReviewedByAdminId, the payment/refund foreign keys and the
/// refund status snapshot: GET /trips/{id} is also readable by the assigned driver, and none of that
/// is theirs to see.
/// </summary>
public record TripRefundIssueDto(
    Guid Id,
    string RequestType,
    string Status,
    bool IsOpen,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc);

public static class TripRefundIssueDtoMapper
{
    /// <summary>
    /// Named ToTripDto rather than ToDto: <c>RefundIssueMapper.ToDto</c> is already an applicable
    /// zero-argument extension on <see cref="RefundIssue"/>, and a second one would make
    /// <c>issue.ToDto()</c> ambiguous wherever both namespaces are in scope.
    /// </summary>
    public static TripRefundIssueDto ToTripDto(this RefundIssue issue) =>
        new(
            issue.Id,
            issue.RequestType.ToString(),
            issue.ReviewStatus.ToString(),
            issue.ReviewStatus.IsOpen(),
            issue.CreatedAtUtc,
            issue.ReviewedAtUtc);
}
