namespace Taxi.Domain.RefundIssues;

public static class RefundIssueReviewStatusExtensions
{
    /// <summary>
    /// True while a refund issue still occupies the passenger's single open slot for a trip, i.e.
    /// the passenger may not submit another review request for the same trip until an admin
    /// resolves or dismisses this one.
    ///
    /// Three places encode this rule and must change together:
    ///   1. this method (read side — TripRefundIssueDto.IsOpen),
    ///   2. the duplicate guard in SubmitRefundIssueCommandHandler (expanded inline so EF can
    ///      translate the predicate to SQL),
    ///   3. the HasFilter on IX_RefundIssues_TripId_PassengerId_Open in RefundIssueConfiguration.
    /// </summary>
    public static bool IsOpen(this RefundIssueReviewStatus status) =>
        status is RefundIssueReviewStatus.Open or RefundIssueReviewStatus.InReview;
}
