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
    IClientConfigProvider clientConfig,
    IStripePaymentService stripe,
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
            if (clientConfig.GetClientConfig().StripeEnabled &&
                payment?.Status == PaymentStatus.Completed &&
                !string.IsNullOrWhiteSpace(payment.StripePaymentIntentId) &&
                claim.RequestedAmount > 0)
            {
                var refundResult = await stripe.CreateRefundAsync(payment.StripePaymentIntentId, claim.RequestedAmount, ct);
                if (refundResult.IsFailure)
                {
                    logger.LogWarning(
                        "Failed to issue compensation refund for PaymentIntent {PaymentIntentId} on claim {ClaimId}",
                        payment.StripePaymentIntentId,
                        claim.Id);
                }
            }
        }

        await context.SaveChangesAsync(ct);
        return claim.ToDto();
    }
}
