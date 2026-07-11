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
    ITripRefundSplitter refundSplitter,
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

        // Split the full refund across all captured fare payments (wallet-first, then card) so
        // mixed trips are fully refunded. No StripeEnabled gate: wallet portions reverse via the
        // wallet ledger and card portions via Stripe.
        if (refundAmount > 0)
        {
            var splitResult = await refundSplitter.RefundAsync(
                new TripRefundSplitRequest(
                    trip.Id,
                    refundAmount,
                    PaymentRefundSourceType.NoDriverCancellation,
                    PassengerId: trip.PassengerId,
                    RefundPercent: refundPercent,
                    TripCancellationId: cancellationResult.Value.Id),
                ct);

            trackedRefund = splitResult.Primary;
            if (!splitResult.AnyCreated || splitResult.AnyFailed)
            {
                logger.LogWarning(
                    "No-driver refund incomplete for trip {TripId}: created={AnyCreated} anyFailed={AnyFailed} refunded={Refunded}",
                    trip.Id,
                    splitResult.AnyCreated,
                    splitResult.AnyFailed,
                    splitResult.TotalRefunded);
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
