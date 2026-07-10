using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.NoDriverCancelTrip;

public class NoDriverCancelTripCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IRefundLifecycleService refundLifecycle,
    TimeProvider timeProvider,
    ILogger<NoDriverCancelTripCommandHandler> logger)
    : IRequestHandler<NoDriverCancelTripCommand, Result<TripDto>>
{
    public async Task<Result<TripDto>> Handle(NoDriverCancelTripCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (trip.PassengerId != passengerId)
        {
            return TripErrors.NotOwnedByPassenger;
        }

        // The 100% refund is granted ONLY for a genuine no-driver prompt. This
        // server-side check is the authority — a client cannot obtain a full
        // refund by hitting this endpoint on an ordinary cancellable trip.
        if (trip.Status != TripStatus.AwaitingAdminAcceptance || !trip.NoDriverDecisionRequired)
        {
            return TripErrors.CannotCancel;
        }

        var quote = await context.PricingQuotes.FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);
        var fare = quote?.FinalFare ?? 0;
        var currency = quote?.CurrencyCode ?? "EUR";

        const decimal refundPercent = 100m;
        var refundAmount = Math.Round(fare * refundPercent / 100m, 2, MidpointRounding.AwayFromZero);

        var payment = await context.Payments
            .FirstOrDefaultAsync(p => p.TripId == trip.Id && p.Kind == PaymentKind.Fare, ct);

        var cancelResult = trip.Cancel();
        if (cancelResult.IsError)
        {
            return cancelResult.Errors;
        }

        var cancellationResult = TripCancellation.Create(
            Guid.NewGuid(),
            trip.Id,
            CancellationActor.Passenger,
            CancellationReason.NoDriverAvailable,
            refundPercent,
            refundAmount,
            currency,
            request.Note);

        if (cancellationResult.IsError)
        {
            return cancellationResult.Errors;
        }

        context.TripCancellations.Add(cancellationResult.Value);

        PaymentRefund? trackedRefund = null;

        // No StripeEnabled gate: RequestRefundAsync reverses wallet payments via the
        // wallet ledger (no Stripe needed) and card payments via Stripe — identical
        // to the normal CancelTripCommandHandler. Gating on StripeEnabled would skip
        // wallet refunds when Stripe is disabled.
        if (payment is not null &&
            payment.Status == PaymentStatus.Completed &&
            refundAmount > 0)
        {
            var refundResult = await refundLifecycle.RequestRefundAsync(
                new RefundRequest(
                    payment.Id,
                    refundAmount,
                    PaymentRefundSourceType.NoDriverCancellation,
                    refundPercent,
                    IsFullRefund: true,
                    trip.Id,
                    cancellationResult.Value.Id,
                    PassengerId: trip.PassengerId),
                ct);

            if (refundResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to request no-driver full refund for trip {TripId}: {ErrorCode}",
                    trip.Id,
                    refundResult.Error.Code);
            }
            else
            {
                trackedRefund = refundResult.Value;
            }
        }

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TripErrors.InvalidStatus(trip.Status);
        }

        return await TripDtoBuilder.BuildAsync(context, trip, timeProvider.GetUtcNow(), ct);
    }
}
