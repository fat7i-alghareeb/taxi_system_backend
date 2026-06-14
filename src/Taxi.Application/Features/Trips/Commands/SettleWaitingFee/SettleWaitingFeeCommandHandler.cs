using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.SettleWaitingFee;

public class SettleWaitingFeeCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IClientConfigProvider clientConfig,
    IStripePaymentService stripe)
    : IRequestHandler<SettleWaitingFeeCommand, Result<WaitingFeeSettlementDto>>
{
    public async Task<Result<WaitingFeeSettlementDto>> Handle(SettleWaitingFeeCommand request, CancellationToken ct)
    {
        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (!currentUser.IsAdmin)
        {
            if (!Guid.TryParse(currentUser.Id, out var passengerId))
            {
                return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
            }

            if (trip.PassengerId != passengerId)
            {
                return TripErrors.NotOwnedByPassenger;
            }
        }

        // Outstanding = total accrued waiting fee − whatever has already been collected.
        var waitingTotal = await context.TripWaitingSessions
            .Where(s => s.TripId == trip.Id)
            .SumAsync(s => s.EstimatedFee ?? 0m, ct);

        var collected = await context.Payments
            .Where(p => p.TripId == trip.Id
                && p.Kind == PaymentKind.WaitingFee
                && p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount, ct);

        var outstanding = Math.Round(waitingTotal - collected, 2, MidpointRounding.AwayFromZero);

        // Currency follows the original fare payment (fallback EUR).
        var farePayment = await context.Payments
            .Where(p => p.TripId == trip.Id && p.Kind == PaymentKind.Fare)
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
        var currency = farePayment?.Currency ?? "EUR";

        if (outstanding <= 0m || !clientConfig.GetClientConfig().StripeEnabled)
        {
            // Nothing to pay (or Stripe disabled): return the amount with no payment sheet.
            return new WaitingFeeSettlementDto(Math.Max(0m, outstanding), currency, null);
        }

        var passenger = await context.DomainUsers.FirstOrDefaultAsync(u => u.Id == trip.PassengerId, ct);
        if (passenger is null)
        {
            return TripErrors.PassengerNotFound;
        }

        var intentResult = await stripe.CreateWaitingFeePaymentIntentAsync(
            amount: outstanding,
            currency: currency,
            tripId: trip.Id,
            passengerId: trip.PassengerId,
            existingStripeCustomerId: passenger.StripeCustomerId,
            passengerEmail: passenger.Email,
            passengerPhone: passenger.Phone,
            passengerName: passenger.Name,
            ct: ct);

        if (intentResult.IsFailure)
        {
            return intentResult.Error;
        }

        var intent = intentResult.Value;

        if (string.IsNullOrWhiteSpace(passenger.StripeCustomerId))
        {
            passenger.SetStripeCustomerId(intent.CustomerId);
        }

        // Upsert a Pending WaitingFee payment for this intent so the success webhook
        // can mark it collected. The intent is idempotent per trip, so a repeated
        // settle call reuses the same intent id (and thus the same payment row).
        var existing = await context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == intent.PaymentIntentId, ct);
        if (existing is null)
        {
            var paymentResult = Payment.CreateWaitingFeeSurcharge(
                Guid.NewGuid(), trip.Id, outstanding, currency, intent.PaymentIntentId);
            if (paymentResult.IsSuccess)
            {
                context.Payments.Add(paymentResult.Value);
            }
        }

        await context.SaveChangesAsync(ct);

        var stripePayment = new StripePaymentDto(
            intent.PaymentIntentId,
            intent.ClientSecret,
            intent.PublishableKey,
            intent.CustomerId,
            intent.EphemeralKeySecret);

        return new WaitingFeeSettlementDto(outstanding, currency, stripePayment);
    }
}
