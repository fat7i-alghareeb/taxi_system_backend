using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.RefundIssues.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.RefundIssues;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.RefundIssues.Commands.SubmitRefundIssue;

public sealed class SubmitRefundIssueCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    INotificationService notificationService,
    ITripNotifier tripNotifier,
    ILogger<SubmitRefundIssueCommandHandler> logger)
    : IRequestHandler<SubmitRefundIssueCommand, Result<RefundIssueDto>>
{
    public async Task<Result<RefundIssueDto>> Handle(SubmitRefundIssueCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var callerId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        if (!Enum.TryParse<RefundIssueRequestType>(request.RequestType, ignoreCase: true, out var requestType))
        {
            return RefundIssueErrors.InvalidRequestType;
        }

        var trip = await context.Trips
            .AsNoTracking()
            .FirstOrDefaultAsync(trip => trip.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (!currentUser.IsAdmin && trip.PassengerId != callerId)
        {
            return TripErrors.NotOwnedByPassenger;
        }

        // One open refund review per passenger per trip. Mirrors the
        // IX_RefundIssues_TripId_PassengerId_Open filtered unique index — the index is the real
        // guarantee, this check is what turns a repeat tap into a clean 409 instead of a
        // constraint violation, and stops the duplicate admin push + SignalR broadcast that made
        // this bug visible in the first place.
        //
        // Placed after the ownership check so NotOwnedByPassenger stays the first observable
        // failure for a non-owner: answering 409 for someone else's trip would reveal whether it
        // has an open refund complaint. Placed before the payment/refund/cancellation loads below
        // because those three round-trips only exist to snapshot values onto a row we are about to
        // refuse to create, and repeat presses are this bug's hot path.
        //
        // Filtered on trip.PassengerId rather than callerId to match what gets written below: an
        // admin may submit on the passenger's behalf, and a callerId predicate would let that path
        // slip past this check straight into the unique index.
        var hasOpenIssue = await context.RefundIssues
            .AsNoTracking()
            .AnyAsync(
                issue => issue.TripId == trip.Id
                    && issue.PassengerId == trip.PassengerId
                    && (issue.ReviewStatus == RefundIssueReviewStatus.Open
                        || issue.ReviewStatus == RefundIssueReviewStatus.InReview),
                ct);
        if (hasOpenIssue)
        {
            return RefundIssueErrors.AlreadyOpen;
        }

        var payment = await context.Payments
            .AsNoTracking()
            .Where(payment => payment.TripId == trip.Id)
            .OrderByDescending(payment => payment.ProcessedAtUtc)
            .ThenByDescending(payment => payment.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        PaymentRefund? latestRefund = null;
        if (payment is not null)
        {
            latestRefund = await context.PaymentRefunds
                .AsNoTracking()
                .Where(refund => refund.PaymentId == payment.Id)
                .OrderByDescending(refund => refund.RequestedAtUtc)
                .ThenByDescending(refund => refund.Id)
                .FirstOrDefaultAsync(ct);
        }

        var latestCancellation = await context.TripCancellations
            .AsNoTracking()
            .Where(cancellation => cancellation.TripId == trip.Id)
            .OrderByDescending(cancellation => cancellation.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        var refundAmountSnapshot = latestRefund?.Amount ?? latestCancellation?.RefundAmount;
        var refundCurrencySnapshot = latestRefund?.Currency ?? latestCancellation?.CurrencyCode;

        var issueResult = RefundIssue.Create(
            Guid.NewGuid(),
            trip.PassengerId,
            trip.Id,
            payment?.Id,
            requestType,
            request.CustomerReason,
            request.Note,
            latestRefund?.Id,
            latestCancellation?.Id,
            latestRefund?.Status,
            refundAmountSnapshot,
            refundCurrencySnapshot,
            request.WhatsAppOpened);
        if (issueResult.IsFailure)
        {
            return issueResult.Errors;
        }

        var issue = issueResult.Value;
        context.RefundIssues.Add(issue);
        await context.SaveChangesAsync(ct);
        await NotifyAdminsAsync(issue, trip.ReferenceCode, ct);
        await NotifyAdminsRealtimeAsync(issue, ct);

        return issue.ToDto(tripReferenceCode: trip.ReferenceCode);
    }

    private async Task NotifyAdminsAsync(RefundIssue issue, string tripReferenceCode, CancellationToken ct)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                ["type"] = "refund_issue_created",
                ["refundIssueId"] = issue.Id.ToString(),
                ["tripId"] = issue.TripId.ToString(),
                ["reviewStatus"] = issue.ReviewStatus.ToString(),
            };
            if (issue.PaymentId.HasValue)
            {
                data["paymentId"] = issue.PaymentId.Value.ToString();
            }

            await notificationService.SendPushNotificationToAdminsAsync(
                LocalizationKeys.RefundIssue.CreatedAdminTitle,
                LocalizationKeys.RefundIssue.CreatedAdminBody,
                data,
                ct,
                bodyArgs: [tripReferenceCode]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to notify admins about refund issue {RefundIssueId}", issue.Id);
        }
    }

    private async Task NotifyAdminsRealtimeAsync(RefundIssue issue, CancellationToken ct)
    {
        try
        {
            await tripNotifier.NotifyRefundIssueCreatedToAdminsAsync(
                issue.Id,
                issue.TripId,
                issue.PassengerId,
                issue.PaymentId,
                issue.RequestType.ToString(),
                issue.ReviewStatus.ToString(),
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast refund issue {RefundIssueId}", issue.Id);
        }
    }
}