using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;

namespace Taxi.Application.Features.Payments.Services;

/// <summary>
/// Silent settlement for a mid-trip fare increase — wallet first, then the default
/// saved card off-session, then the reusable card on the original fare intent.
/// A remainder that can't be covered off-session is reported (not left Unpaid) so the
/// caller opens a PaymentSheet. Mirrors <see cref="FeeSettlementService"/>.
/// </summary>
public sealed class FareAdjustmentSettlementService(
    IAppDbContext context,
    IWalletService wallet,
    IStripePaymentService stripe) : IFareAdjustmentSettlementService
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<FareAdjustmentSettlementOutcome>> SettleAsync(
        Guid tripId,
        Guid passengerId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        var total = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (total <= 0m)
        {
            return new FareAdjustmentSettlementOutcome(0m, 0m, 0m, 0m);
        }

        // 1) Wallet first — debit up to the available balance (idempotent).
        var walletPaid = 0m;
        Guid? walletPaymentId = null;
        var debitResult = await wallet.DebitForFeeAsync(
            passengerId, tripId, total, currency, "Fare adjustment", $"{idempotencyKey}-wallet", ct);
        if (debitResult.IsSuccess && debitResult.Value.DebitedAmount > 0m)
        {
            walletPaid = debitResult.Value.DebitedAmount;
            walletPaymentId = await RecordWalletPaymentAsync(
                tripId, walletPaid, currency, debitResult.Value.WalletTransactionId, ct);
        }

        var remaining = decimal.Round(total - walletPaid, 2, MidpointRounding.AwayFromZero);
        if (remaining <= 0m)
        {
            return new FareAdjustmentSettlementOutcome(total, walletPaid, 0m, 0m, walletPaymentId);
        }

        // 2) Remainder — default saved reusable card off-session, else the reusable card
        //    on the original fare intent. Anything not covered → needs a PaymentSheet.
        var cardPaid = await TryChargeCardAsync(tripId, passengerId, remaining, currency, idempotencyKey, ct);
        var stillRemaining = decimal.Round(remaining - cardPaid, 2, MidpointRounding.AwayFromZero);

        return new FareAdjustmentSettlementOutcome(total, walletPaid, cardPaid, stillRemaining, walletPaymentId);
    }

    private async Task<Guid?> RecordWalletPaymentAsync(
        Guid tripId, decimal amount, string currency, Guid? walletTransactionId, CancellationToken ct)
    {
        var reference = walletTransactionId?.ToString();

        var existing = await _context.Payments.FirstOrDefaultAsync(
            p => p.TripId == tripId
                && p.Kind == PaymentKind.FareAdjustment
                && p.Method == PaymentMethod.Wallet
                && p.TransactionReference == reference,
            ct);
        if (existing is not null)
        {
            return existing.Id;
        }

        var paymentResult = Payment.CreateFareAdjustmentWalletPayment(
            Guid.NewGuid(), tripId, amount, currency, reference);
        if (paymentResult.IsFailure)
        {
            return null;
        }

        var payment = paymentResult.Value;
        payment.MarkAsCompleted();
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);
        return payment.Id;
    }

    private async Task<decimal> TryChargeCardAsync(
        Guid tripId, Guid passengerId, decimal remaining, string currency, string idempotencyKey, CancellationToken ct)
    {
        var passenger = await _context.DomainUsers.FirstOrDefaultAsync(u => u.Id == passengerId, ct);
        var defaultMethod = await _context.PaymentMethods
            .Where(m => m.PassengerId == passengerId && m.IsDefault && m.DeletedAtUtc == null)
            .FirstOrDefaultAsync(ct);

        var cardIdempotencyKey = $"{idempotencyKey}-card";
        Result<StripeSurchargeResult> chargeResult;

        if (defaultMethod is not null && !string.IsNullOrWhiteSpace(passenger?.StripeCustomerId))
        {
            chargeResult = await stripe.ChargeOffSessionAsync(
                passenger.StripeCustomerId!, defaultMethod.GatewayPaymentMethodId, remaining, currency,
                tripId, "fare_adjustment", cardIdempotencyKey, ct);
        }
        else
        {
            var fareIntentId = await _context.Payments
                .Where(p => p.TripId == tripId
                    && p.Kind == PaymentKind.Fare
                    && p.Method == PaymentMethod.CreditCard
                    && p.Status == PaymentStatus.Completed
                    && p.StripePaymentIntentId != null)
                .OrderByDescending(p => p.CreatedAtUtc)
                .Select(p => p.StripePaymentIntentId)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(fareIntentId))
            {
                return 0m; // no reusable card on file — caller falls back to a PaymentSheet
            }

            chargeResult = await stripe.ChargeWaitingFeeAsync(
                fareIntentId, remaining, currency, tripId, cardIdempotencyKey, ct);
        }

        if (chargeResult.IsFailure)
        {
            return 0m; // caller falls back to a PaymentSheet
        }

        var surcharge = chargeResult.Value;

        // Idempotent: the off-session charge is keyed, so a retry returns the same intent id.
        var existing = await _context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == surcharge.PaymentIntentId, ct);
        if (existing is not null)
        {
            return existing.Status == PaymentStatus.Completed ? existing.Amount : 0m;
        }

        // SCA / declines → do NOT record a Failed row; fall back to a PaymentSheet.
        if (!surcharge.Succeeded)
        {
            return 0m;
        }

        var paymentResult = Payment.CreateFareAdjustmentSurcharge(
            Guid.NewGuid(), tripId, remaining, currency, surcharge.PaymentIntentId);
        if (paymentResult.IsFailure)
        {
            return 0m;
        }

        var payment = paymentResult.Value;
        payment.MarkAsCompleted(surcharge.ChargeId);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);

        return remaining;
    }
}
