using System.Text.Json;

using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.ApplyTripEdit;

public sealed class ApplyTripEditCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    ITripRequoteService requoteService,
    ITripEditApplier editApplier,
    IFareAdjustmentSettlementService settlementService,
    IRefundLifecycleService refundService,
    ITripRefundSplitter refundSplitter,
    IStripePaymentService stripe,
    IClientConfigProvider clientConfig,
    TimeProvider timeProvider) : IRequestHandler<ApplyTripEditCommand, Result<TripEditApplyResultDto>>
{
    // Server delta may drift from what the customer confirmed (quote expiry / concurrent edit).
    private const decimal DeltaDriftTolerance = 0.01m;

    public async Task<Result<TripEditApplyResultDto>> Handle(ApplyTripEditCommand request, CancellationToken ct)
    {
        if (request.Stops is null && request.PassengerCount is null)
        {
            return TripErrors.InvalidStops;
        }

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

        if (!TripEditPolicy.IsEditableForRepricing(trip.Status))
        {
            return TripErrors.InvalidStatus(trip.Status);
        }

        var requoteResult = await requoteService.RequoteAsync(trip, request.Stops, request.PassengerCount, ct);
        if (requoteResult.IsFailure)
        {
            return requoteResult.Error;
        }

        var r = requoteResult.Value;

        // Drift guard: the customer confirmed a specific difference. If the fresh re-quote no
        // longer matches (quote expired, a concurrent edit landed), bail so the app re-previews.
        if (Math.Abs(r.Delta - request.ExpectedDelta) > DeltaDriftTolerance)
        {
            return TripErrors.EditDeltaChanged;
        }

        var currency = r.CurrencyCode;
        var newStops = request.Stops is { Count: > 0 } ? r.NewStops : null;

        // No fare change → just apply.
        if (r.Delta == 0m)
        {
            return await CommitAndBuildAsync(trip, r, newStops, request.PassengerCount, 0m, currency, ct);
        }

        // Fare decrease → apply immediately, then refund the difference (customer's favor).
        if (r.Delta < 0m)
        {
            var applied = await CommitAndBuildAsync(trip, r, newStops, request.PassengerCount, r.Delta, currency, ct);
            if (applied.IsFailure)
            {
                return applied.Error;
            }

            await RefundDeltaAsync(trip, Math.Abs(r.Delta), ct);
            return applied;
        }

        // Fare increase → settle silently (wallet → off-session card). If that covers it, apply.
        var idempotencyKey = $"edit-{trip.Id}-{r.NewQuote.Id:N}";
        var settleResult = await settlementService.SettleAsync(
            trip.Id, trip.PassengerId, r.Delta, currency, idempotencyKey, ct);
        if (settleResult.IsFailure)
        {
            return settleResult.Error;
        }

        var outcome = settleResult.Value;
        if (!outcome.RequiresInteractiveSheet)
        {
            return await CommitAndBuildAsync(trip, r, newStops, request.PassengerCount, r.Delta, currency, ct);
        }

        // Couldn't charge silently → hold the edit for an interactive PaymentSheet. The trip is
        // NOT mutated; the success webhook applies it, and failure / expiry reverts it.
        return await HoldForPaymentSheetAsync(trip, r, request, currency, outcome, ct);
    }

    private async Task<Result<TripEditApplyResultDto>> CommitAndBuildAsync(
        Trip trip,
        TripRequoteResult r,
        IReadOnlyList<TripStop>? newStops,
        int? newPassengerCount,
        decimal delta,
        string currency,
        CancellationToken ct)
    {
        context.PricingQuotes.Add(r.NewQuote);

        var apply = await editApplier.ApplyAsync(trip, r.NewQuote, newStops, newPassengerCount, r.NewVehicleTypeId, ct);
        if (apply.IsFailure)
        {
            return apply.Errors;
        }

        await context.SaveChangesAsync(ct);

        var dto = await TripDtoBuilder.BuildAsync(context, trip, timeProvider.GetUtcNow(), ct);
        return TripEditApplyResultDto.Applied(dto, delta, currency);
    }

    private async Task<Result<TripEditApplyResultDto>> HoldForPaymentSheetAsync(
        Trip trip,
        TripRequoteResult r,
        ApplyTripEditCommand request,
        string currency,
        FareAdjustmentSettlementOutcome outcome,
        CancellationToken ct)
    {
        // Without Stripe there is no sheet to present. Reverse any wallet portion so nothing sticks.
        if (!clientConfig.GetClientConfig().StripeEnabled)
        {
            await ReverseWalletPortionAsync(trip, outcome, ct);
            return PaymentErrors.StripeInitiationFailed;
        }

        var passenger = await context.DomainUsers.FirstOrDefaultAsync(u => u.Id == trip.PassengerId, ct);
        if (passenger is null)
        {
            await ReverseWalletPortionAsync(trip, outcome, ct);
            return TripErrors.PassengerNotFound;
        }

        var remainder = outcome.Remaining;
        var pendingId = Guid.NewGuid();
        var idempotencyKey = $"edit-{trip.Id}-{r.NewQuote.Id:N}-sheet";

        var intentResult = await stripe.CreateFareAdjustmentPaymentIntentAsync(
            amount: remainder,
            currency: currency,
            tripId: trip.Id,
            passengerId: trip.PassengerId,
            pendingEditId: pendingId,
            idempotencyKey: idempotencyKey,
            existingStripeCustomerId: passenger.StripeCustomerId,
            passengerEmail: passenger.Email,
            passengerPhone: passenger.Phone,
            passengerName: passenger.Name,
            ct: ct);
        if (intentResult.IsFailure)
        {
            await ReverseWalletPortionAsync(trip, outcome, ct);
            return intentResult.Error;
        }

        var intent = intentResult.Value;
        if (string.IsNullOrWhiteSpace(passenger.StripeCustomerId))
        {
            passenger.SetStripeCustomerId(intent.CustomerId);
        }

        // Persist the new quote UNUSED (the webhook marks it used on apply; the sweeper releases it
        // if the sheet is abandoned).
        context.PricingQuotes.Add(r.NewQuote);

        var kind = request.PassengerCount.HasValue ? PendingTripEditKind.Passenger : PendingTripEditKind.Stops;
        var proposedStopsJson = request.Stops is { Count: > 0 }
            ? JsonSerializer.Serialize(request.Stops)
            : null;

        var pending = PendingTripEdit.Create(
            pendingId,
            trip.Id,
            trip.PassengerId,
            kind,
            proposedStopsJson,
            request.PassengerCount,
            r.NewQuote.Id,
            r.NewVehicleTypeId,
            r.Delta,
            currency,
            intent.PaymentIntentId,
            new DateTimeOffset(DateTime.SpecifyKind(r.NewQuote.ValidUntil, DateTimeKind.Utc)),
            outcome.WalletPaid,
            outcome.WalletPaymentId);
        context.PendingTripEdits.Add(pending);

        // Pending card payment for the remainder; the success webhook marks it Completed.
        var existingPayment = await context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == intent.PaymentIntentId, ct);
        if (existingPayment is null)
        {
            var paymentResult = Payment.CreateFareAdjustmentSurcharge(
                Guid.NewGuid(), trip.Id, remainder, currency, intent.PaymentIntentId);
            if (paymentResult.IsSuccess)
            {
                context.Payments.Add(paymentResult.Value);
            }
        }

        await context.SaveChangesAsync(ct);

        var sheet = new StripePaymentDto(
            intent.PaymentIntentId,
            intent.ClientSecret,
            intent.PublishableKey,
            intent.CustomerId,
            intent.EphemeralKeySecret);

        return TripEditApplyResultDto.RequiresSheet(r.Delta, currency, pendingId, sheet);
    }

    /// <summary>
    /// Refunds a fare DECREASE across the trip's captured fare money, wallet-funded portion first
    /// (ledger reversal) then card (Stripe), one refund request per source payment.
    /// </summary>
    private async Task RefundDeltaAsync(Trip trip, decimal amount, CancellationToken ct)
    {
        await refundSplitter.RefundAsync(
            new TripRefundSplitRequest(
                trip.Id,
                amount,
                PaymentRefundSourceType.FareAdjustment,
                PassengerId: trip.PassengerId),
            ct);
    }

    private async Task ReverseWalletPortionAsync(Trip trip, FareAdjustmentSettlementOutcome outcome, CancellationToken ct)
    {
        if (outcome.WalletPaymentId is not Guid walletPaymentId || outcome.WalletPaid <= 0m)
        {
            return;
        }

        var refundRequest = new RefundRequest(
            walletPaymentId,
            outcome.WalletPaid,
            PaymentRefundSourceType.FareAdjustment,
            TripId: trip.Id,
            PassengerId: trip.PassengerId);
        await refundService.RequestRefundAsync(refundRequest, ct);
    }
}
