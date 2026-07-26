using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.RefundIssues;

using Xunit;

namespace Taxi.Application.UnitTests.RefundIssues;

/// <summary>
/// Covers the customer-facing projection of a refund issue onto <see cref="TripDto"/>. The rule
/// "which statuses still block a resubmit" is encoded in three places — this method, the guard in
/// SubmitRefundIssueCommandHandler, and the filtered unique index — so it is pinned here as well as
/// in the handler tests.
/// </summary>
public class TripRefundIssueDtoTests
{
    [Theory]
    [InlineData(RefundIssueReviewStatus.Open)]
    [InlineData(RefundIssueReviewStatus.InReview)]
    public void IsOpen_WhileAwaitingAnAdmin_IsTrue(RefundIssueReviewStatus status)
    {
        Assert.True(status.IsOpen());
    }

    [Theory]
    [InlineData(RefundIssueReviewStatus.Resolved)]
    [InlineData(RefundIssueReviewStatus.Dismissed)]
    public void IsOpen_OnceTheAdminHasClosedIt_IsFalse(RefundIssueReviewStatus status)
    {
        Assert.False(status.IsOpen());
    }

    [Fact]
    public void ToTripDto_ForAFreshIssue_ReportsItAsOpenAndUnreviewed()
    {
        var issue = CreateIssue();

        var dto = issue.ToTripDto();

        Assert.Equal(issue.Id, dto.Id);
        Assert.Equal(nameof(RefundIssueRequestType.DidNotReceiveRefund), dto.RequestType);
        Assert.Equal(nameof(RefundIssueReviewStatus.Open), dto.Status);
        Assert.True(dto.IsOpen);
        Assert.Equal(issue.CreatedAtUtc, dto.CreatedAtUtc);
        Assert.Null(dto.ReviewedAtUtc);
    }

    [Theory]
    [InlineData(RefundIssueReviewStatus.Resolved)]
    [InlineData(RefundIssueReviewStatus.Dismissed)]
    public void ToTripDto_OnceClosed_FreesTheSlotAndCarriesTheReviewDate(
        RefundIssueReviewStatus status)
    {
        var issue = CreateIssue();
        issue.Review(status, Guid.NewGuid(), adminNotes: "handled");

        var dto = issue.ToTripDto();

        Assert.Equal(status.ToString(), dto.Status);
        Assert.False(dto.IsOpen);
        Assert.NotNull(dto.ReviewedAtUtc);
    }

    [Fact]
    public void ToTripDto_NeverCarriesTheAdminOrPassengerNotes()
    {
        // GET /trips/{id} is also served to the assigned driver, so the projection must stay
        // narrower than the admin RefundIssueDto it is derived from.
        var issue = CreateIssue(note: "The refund never arrived and I am furious.");
        issue.Review(RefundIssueReviewStatus.Resolved, Guid.NewGuid(), adminNotes: "refunded manually");

        var dto = issue.ToTripDto();

        Assert.DoesNotContain("furious", dto.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("manually", dto.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static RefundIssue CreateIssue(string? note = null) =>
        RefundIssue.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            paymentId: null,
            RefundIssueRequestType.DidNotReceiveRefund,
            "did-not-receive",
            note).Value;
}
