using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.RefundIssues;
using Taxi.Application.Features.RefundIssues.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.RefundIssues;

namespace Taxi.Application.Features.RefundIssues.Commands.ReviewRefundIssue;

public sealed class ReviewRefundIssueCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<ReviewRefundIssueCommand, Result<RefundIssueDto>>
{
    public async Task<Result<RefundIssueDto>> Handle(ReviewRefundIssueCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var adminId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        if (!Enum.TryParse<RefundIssueReviewStatus>(request.ReviewStatus, ignoreCase: true, out var status))
        {
            return RefundIssueErrors.InvalidReviewStatus;
        }

        var issue = await context.RefundIssues.FirstOrDefaultAsync(issue => issue.Id == request.Id, ct);
        if (issue is null)
        {
            return RefundIssueErrors.NotFound;
        }

        var review = issue.Review(status, adminId, request.AdminNotes);
        if (review.IsFailure)
        {
            return review.Errors;
        }

        await context.SaveChangesAsync(ct);
        var items = await RefundIssueDtoProjector.ToDtosAsync(context, [issue], ct);
        return items[0];
    }
}
