using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.ReviewCompensationClaim;

public sealed class ReviewCompensationClaimCommandHandler(
    IAppDbContext context,
    ITripRefundSplitter refundSplitter,
    ILogger<ReviewCompensationClaimCommandHandler> logger)
    : IRequestHandler<ReviewCompensationClaimCommand, Result<CompensationClaimDto>>
{
    public async Task<Result<CompensationClaimDto>> Handle(ReviewCompensationClaimCommand request, CancellationToken ct)
    {
        var claim = await context.TripCompensationClaims.FirstOrDefaultAsync(c => c.Id == request.ClaimId, ct);
        if (claim is null)
        {
            return TripErrors.NotFound;
        }

        var reviewResult = request.Approved ? claim.Approve(request.Notes) : claim.Reject(request.Notes);
        if (reviewResult.IsError)
        {
            return reviewResult.Errors;
        }

        if (request.Approved && claim.RequestedAmount > 0)
        {
            // Split the approved amount across all captured fare payments (wallet-first, then
            // card) so mixed / wallet-only trips are compensated correctly.
            var splitResult = await refundSplitter.RefundAsync(
                new TripRefundSplitRequest(
                    claim.TripId,
                    claim.RequestedAmount,
                    PaymentRefundSourceType.CompensationClaim,
                    PassengerId: claim.PassengerId,
                    TripCompensationClaimId: claim.Id),
                ct);

            if (!splitResult.AnyCreated || splitResult.AnyFailed)
            {
                logger.LogWarning(
                    "Compensation refund incomplete for claim {ClaimId} (trip {TripId}): created={AnyCreated} anyFailed={AnyFailed} refunded={Refunded}",
                    claim.Id,
                    claim.TripId,
                    splitResult.AnyCreated,
                    splitResult.AnyFailed,
                    splitResult.TotalRefunded);
            }
        }

        await context.SaveChangesAsync(ct);
        return claim.ToDto();
    }
}
