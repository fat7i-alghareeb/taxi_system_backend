using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.CancelTrip;

public class CancelTripCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IClientConfigProvider clientConfig,
    IStripePaymentService stripe,
    ILogger<CancelTripCommandHandler> logger) : IRequestHandler<CancelTripCommand, Result<TripDto>>
{
    public async Task<Result<TripDto>> Handle(CancelTripCommand request, CancellationToken ct)
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

        if (trip.Status is TripStatus.InProgress
            or TripStatus.Completed
            or TripStatus.Cancelled
            or TripStatus.Refunded
            or TripStatus.PaymentFailed)
        {
            return TripErrors.CannotCancel;
        }

        var preCancelStatus = trip.Status;

        // Load the linked Payment (if any) so we know whether to refund or cancel the PaymentIntent.
        var payment = await context.Payments
            .FirstOrDefaultAsync(p => p.TripId == trip.Id, ct);

        var cancelResult = trip.Cancel();
        if (cancelResult.IsError)
        {
            return cancelResult.Errors;
        }

        var stripeEnabled = clientConfig.GetClientConfig().StripeEnabled;

        if (stripeEnabled && payment is not null && !string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
        {
            if (preCancelStatus == TripStatus.AwaitingPayment && payment.Status == PaymentStatus.Pending)
            {
                // PaymentSheet was never completed: cancel the intent so the user is never charged.
                var cancelIntentResult = await stripe.CancelPaymentIntentAsync(payment.StripePaymentIntentId, ct);
                if (cancelIntentResult.IsFailure)
                {
                    // Log but do not block trip cancellation: the trip is already cancelled in our domain.
                    logger.LogWarning(
                        "Failed to cancel Stripe PaymentIntent {PaymentIntentId} for trip {TripId}",
                        payment.StripePaymentIntentId,
                        trip.Id);
                }
            }
            else if (payment.Status == PaymentStatus.Completed
                && preCancelStatus is TripStatus.PendingDriver
                    or TripStatus.Scheduled
                    or TripStatus.DriverAssigned)
            {
                // Pre-dispatch refund per plan: auto-refund up to and including DriverAssigned.
                // The charge.refunded webhook will then transition Payment→Refunded and Trip→Refunded.
                var refundResult = await stripe.CreateRefundAsync(payment.StripePaymentIntentId, ct);
                if (refundResult.IsFailure)
                {
                    logger.LogWarning(
                        "Failed to issue Stripe refund for PaymentIntent {PaymentIntentId} on trip {TripId}",
                        payment.StripePaymentIntentId,
                        trip.Id);
                }
            }
        }

        await context.SaveChangesAsync(ct);

        var quote = await context.PricingQuotes.FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);

        var stopDtos = trip.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new TripStopDto(s.Coordinate.Latitude, s.Coordinate.Longitude))
            .ToList();

        return new TripDto(
            trip.Id,
            trip.ReferenceCode,
            trip.PassengerId,
            trip.DriverId,
            trip.VehicleTypeId,
            trip.Status.ToString(),
            quote?.FinalFare ?? 0,
            quote?.CurrencyCode ?? "EUR",
            trip.CreatedAtUtc,
            trip.ScheduledAtUtc,
            stopDtos);
    }
}
