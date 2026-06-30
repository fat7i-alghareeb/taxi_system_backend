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
    IRefundLifecycleService refundLifecycle,
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

        if (request.Approved)
        {
            var payment = await context.Payments.FirstOrDefaultAsync(
                p => p.TripId == claim.TripId && p.Kind == PaymentKind.Fare, ct);
            if (payment?.Status == PaymentStatus.Completed &&
                !string.IsNullOrWhiteSpace(payment.StripePaymentIntentId) &&
                claim.RequestedAmount > 0)
            {
                var refundPercent = payment.Amount > 0
                    ? Math.Round(claim.RequestedAmount / payment.Amount * 100m, 2, MidpointRounding.AwayFromZero)
                    : (decimal?)null;
                var refundResult = await refundLifecycle.RequestRefundAsync(
                    new RefundRequest(
                        payment.Id,
                        claim.RequestedAmount,
                        PaymentRefundSourceType.CompensationClaim,
                        refundPercent,
                        claim.RequestedAmount >= payment.Amount,
                        claim.TripId,
                        TripCompensationClaimId: claim.Id,
                        PassengerId: claim.PassengerId),
                    ct);

                if (refundResult.IsFailure)
                {
                    logger.LogWarning(
                        "Failed to request tracked compensation refund for PaymentIntent {PaymentIntentId} on claim {ClaimId}: {ErrorCode}",
                        payment.StripePaymentIntentId,
                        claim.Id,
                        refundResult.Error.Code);
                }
                else if (refundResult.Value.Status == PaymentRefundStatus.Failed)
                {
                    logger.LogWarning(
                        "Tracked compensation refund {RefundId} failed immediately for PaymentIntent {PaymentIntentId} on claim {ClaimId}",
                        refundResult.Value.Id,
                        payment.StripePaymentIntentId,
                        claim.Id);
                }
            }
        }

        await context.SaveChangesAsync(ct);
        return claim.ToDto();
    }
}
