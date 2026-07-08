using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;

namespace Taxi.Application.Features.Payments.Services;

public sealed class FeeSettlementService(
    IAppDbContext context,
    IWalletService wallet,
    IStripePaymentService stripe) : IFeeSettlementService
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<FeeSettlementOutcome>> SettleWaitingFeeAsync(
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
            return new FeeSettlementOutcome(0m, 0m, 0m, 0m, currency);
        }

        // 1) Wallet first — debit up to the available balance (idempotent).
        var walletPaid = 0m;
        var debitResult = await wallet.DebitForFeeAsync(
            passengerId, tripId, total, currency, "Waiting fee", $"{idempotencyKey}-wallet", ct);
        if (debitResult.IsSuccess && debitResult.Value.DebitedAmount > 0m)
        {
            walletPaid = debitResult.Value.DebitedAmount;
            await RecordWalletFeePaymentAsync(
                tripId, walletPaid, currency, debitResult.Value.WalletTransactionId, ct);
        }

        var remaining = decimal.Round(total - walletPaid, 2, MidpointRounding.AwayFromZero);
        if (remaining <= 0m)
        {
            return new FeeSettlementOutcome(total, walletPaid, 0m, 0m, currency);
        }

        // 2) Remainder — the default saved reusable card (off-session), else Unpaid.
        var cardPaid = await TryChargeCardAsync(tripId, passengerId, remaining, currency, idempotencyKey, ct);
        var unpaid = decimal.Round(remaining - cardPaid, 2, MidpointRounding.AwayFromZero);

        return new FeeSettlementOutcome(total, walletPaid, cardPaid, unpaid, currency);
    }

    private async Task RecordWalletFeePaymentAsync(
        Guid tripId,
        decimal amount,
        string currency,
        Guid? walletTransactionId,
        CancellationToken ct)
    {
        var reference = walletTransactionId?.ToString();

        // Idempotent: don't double-record the wallet-funded portion for the same ledger entry.
        var alreadyRecorded = await _context.Payments.AnyAsync(
            p => p.TripId == tripId
                && p.Kind == PaymentKind.WaitingFee
                && p.Method == PaymentMethod.Wallet
                && p.TransactionReference == reference,
            ct);
        if (alreadyRecorded)
        {
            return;
        }

        var paymentResult = Payment.CreateWaitingFeeWalletPayment(
            Guid.NewGuid(), tripId, amount, currency, reference);
        if (paymentResult.IsFailure)
        {
            return;
        }

        var payment = paymentResult.Value;
        payment.MarkAsCompleted();
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);
    }

    private async Task<decimal> TryChargeCardAsync(
        Guid tripId,
        Guid passengerId,
        decimal remaining,
        string currency,
        string idempotencyKey,
        CancellationToken ct)
    {
        var passenger = await _context.DomainUsers.FirstOrDefaultAsync(u => u.Id == passengerId, ct);
        var defaultMethod = await _context.PaymentMethods
            .Where(m => m.PassengerId == passengerId && m.IsDefault && m.DeletedAtUtc == null)
            .FirstOrDefaultAsync(ct);

        var cardIdempotencyKey = $"{idempotencyKey}-card";
        Result<StripeSurchargeResult> chargeResult;

        if (defaultMethod is not null && !string.IsNullOrWhiteSpace(passenger?.StripeCustomerId))
        {
            // Preferred: the explicitly-saved default reusable card.
            chargeResult = await stripe.ChargeOffSessionAsync(
                passenger.StripeCustomerId!, defaultMethod.GatewayPaymentMethodId, remaining, currency,
                tripId, "waiting_fee", cardIdempotencyKey, ct);
        }
        else
        {
            // Fallback: reuse the reusable card saved on the original fare payment. If the fare was
            // paid by a one-off method (or there is no card at all), the remainder stays Unpaid.
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
                return 0m; // no reusable card on file — Unpaid
            }

            chargeResult = await stripe.ChargeWaitingFeeAsync(
                fareIntentId, remaining, currency, tripId, cardIdempotencyKey, ct);
        }

        if (chargeResult.IsFailure)
        {
            // Hard failure with no PaymentIntent to record — remainder is Unpaid.
            return 0m;
        }

        var surcharge = chargeResult.Value;

        // Idempotent: the off-session charge is keyed, so a retry returns the same intent id.
        var existing = await _context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == surcharge.PaymentIntentId, ct);
        if (existing is not null)
        {
            return existing.Status == PaymentStatus.Completed ? existing.Amount : 0m;
        }

        var paymentResult = Payment.CreateWaitingFeeSurcharge(
            Guid.NewGuid(), tripId, remaining, currency, surcharge.PaymentIntentId);
        if (paymentResult.IsFailure)
        {
            return 0m;
        }

        var payment = paymentResult.Value;
        if (surcharge.Succeeded)
        {
            payment.MarkAsCompleted(surcharge.ChargeId);
        }
        else
        {
            payment.MarkAsFailed(
                surcharge.RequiresAction ? "authentication_required" : surcharge.Status,
                "Off-session waiting-fee charge was not completed.");
        }

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);

        return surcharge.Succeeded ? remaining : 0m;
    }
}
