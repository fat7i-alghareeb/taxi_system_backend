using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Npgsql;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Wallet;
using Taxi.Infrastructure.Data;

namespace Taxi.Infrastructure.Wallet;

/// <summary>
/// Backend-owned wallet ledger. Depends on the concrete <see cref="AppDbContext"/> so it can
/// use optimistic-concurrency reload/retry for balance updates. All money movements are
/// recorded as <see cref="WalletTransaction"/> rows; the account's materialized balance is
/// only ever changed alongside a committed ledger entry in the same unit of work.
/// </summary>
public sealed class WalletService(AppDbContext db, ILogger<WalletService> logger) : IWalletService
{
    private const int MaxConcurrencyRetries = 5;

    public async Task<Result<Success>> CreatePendingTopUpAsync(
        Guid walletTransactionId,
        Guid userId,
        decimal amount,
        string currency,
        string stripePaymentIntentId,
        CancellationToken ct = default)
    {
        var accountResult = await GetOrCreateAccountAsync(userId, currency, ct);
        if (accountResult.IsFailure)
        {
            return accountResult.Error;
        }

        var account = accountResult.Value;
        var idempotencyKey = $"wallet-topup-{walletTransactionId:N}";

        var txnResult = WalletTransaction.CreatePendingTopUp(
            walletTransactionId,
            account.Id,
            amount,
            account.Currency,
            stripePaymentIntentId,
            idempotencyKey);
        if (txnResult.IsFailure)
        {
            return txnResult.Error;
        }

        db.WalletTransactions.Add(txnResult.Value);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<Result<WalletTopUpCreditOutcome>> CreditTopUpFromWebhookAsync(
        string stripePaymentIntentId,
        decimal? amountReceived,
        string? stripeChargeId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(stripePaymentIntentId))
        {
            return NotAWalletTopUp();
        }

        var txn = await db.WalletTransactions
            .FirstOrDefaultAsync(
                t => t.StripePaymentIntentId == stripePaymentIntentId && t.Type == WalletTransactionType.TopUp,
                ct);

        if (txn is null)
        {
            // Not a wallet top-up PaymentIntent — let the caller handle it (e.g. trip payment).
            return NotAWalletTopUp();
        }

        var account = await db.WalletAccounts.FirstOrDefaultAsync(a => a.Id == txn.WalletAccountId, ct);
        if (account is null)
        {
            return WalletErrors.AccountNotFound;
        }

        if (amountReceived.HasValue && decimal.Round(amountReceived.Value, 2) != decimal.Round(txn.Amount, 2))
        {
            // Should not happen (PI amount == recorded amount). Credit the recorded amount and flag.
            logger.LogWarning(
                "Wallet top-up amount mismatch for intent {IntentId}: recorded {Recorded}, Stripe reported {Received}. Crediting recorded amount.",
                stripePaymentIntentId,
                txn.Amount,
                amountReceived.Value);
        }

        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            if (txn.Status == WalletTransactionStatus.Committed)
            {
                return new WalletTopUpCreditOutcome(
                    WalletTopUpCreditStatus.AlreadyCredited,
                    account.UserId,
                    txn.Amount,
                    account.Balance,
                    account.Currency);
            }

            var creditResult = account.Credit(txn.Amount);
            if (creditResult.IsFailure)
            {
                return creditResult.Error;
            }

            txn.MarkCommitted(account.Balance, stripeChargeId);

            try
            {
                await db.SaveChangesAsync(ct);
                return new WalletTopUpCreditOutcome(
                    WalletTopUpCreditStatus.Credited,
                    account.UserId,
                    txn.Amount,
                    account.Balance,
                    account.Currency);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries - 1)
            {
                // A concurrent writer changed the account row. Discard our in-memory changes,
                // reload the current state and retry. If the other writer already committed
                // this top-up, the next iteration returns AlreadyCredited.
                await db.Entry(account).ReloadAsync(ct);
                await db.Entry(txn).ReloadAsync(ct);
            }
        }

        // Exhausted retries: surface as a failure so the webhook returns non-2xx and Stripe retries.
        logger.LogError(
            "Wallet top-up crediting for intent {IntentId} failed after {Retries} concurrency retries.",
            stripePaymentIntentId,
            MaxConcurrencyRetries);
        return WalletErrors.AccountNotFound;
    }

    public async Task<Result<WalletDebitOutcome>> DebitForFeeAsync(
        Guid userId,
        Guid tripId,
        decimal amount,
        string currency,
        string description,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (amount <= 0m)
        {
            return new WalletDebitOutcome(0m, null);
        }

        var rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

        // Idempotency: this fee debit already posted — return the amount already taken.
        var existing = await db.WalletTransactions
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null)
        {
            var already = existing.Status == WalletTransactionStatus.Committed ? existing.Amount : 0m;
            return new WalletDebitOutcome(already, existing.Id);
        }

        var account = await db.WalletAccounts.FirstOrDefaultAsync(a => a.UserId == userId, ct);
        if (account is null)
        {
            return new WalletDebitOutcome(0m, null); // no wallet — nothing to debit
        }

        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            if (account.Balance <= 0m)
            {
                return new WalletDebitOutcome(0m, null);
            }

            var portion = Math.Min(account.Balance, rounded);

            var txnResult = WalletTransaction.CreateFeeCharge(
                Guid.NewGuid(), account.Id, portion, currency, idempotencyKey,
                tripId: tripId, description: description);
            if (txnResult.IsFailure)
            {
                return txnResult.Error;
            }

            var txn = txnResult.Value;
            var debitResult = account.Debit(portion);
            if (debitResult.IsFailure)
            {
                return debitResult.Error;
            }

            txn.MarkCommitted(account.Balance);
            db.WalletTransactions.Add(txn);

            try
            {
                await db.SaveChangesAsync(ct);
                return new WalletDebitOutcome(portion, txn.Id);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // A concurrent identical debit already posted — treat as idempotent.
                db.Entry(txn).State = EntityState.Detached;
                await db.Entry(account).ReloadAsync(ct);
                var posted = await db.WalletTransactions
                    .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);
                return new WalletDebitOutcome(posted?.Amount ?? 0m, posted?.Id);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries - 1)
            {
                // Balance changed under us — reload and recompute the portion.
                db.Entry(txn).State = EntityState.Detached;
                await db.Entry(account).ReloadAsync(ct);
            }
        }

        logger.LogError("Wallet fee debit for trip {TripId} failed after {Retries} concurrency retries.", tripId, MaxConcurrencyRetries);
        return WalletErrors.InsufficientBalance;
    }

    public async Task<Result<Success>> CreditRefundReversalAsync(
        Guid userId,
        Guid tripId,
        Guid paymentId,
        Guid paymentRefundId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (amount <= 0m)
        {
            return WalletErrors.InvalidAmount;
        }

        // Idempotency: reversal already posted.
        var existing = await db.WalletTransactions
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null)
        {
            return Result.Success;
        }

        var accountResult = await GetOrCreateAccountAsync(userId, currency, ct);
        if (accountResult.IsFailure)
        {
            return accountResult.Error;
        }

        var account = accountResult.Value;

        var txnResult = WalletTransaction.CreateRefundReversal(
            Guid.NewGuid(), account.Id, amount, currency, idempotencyKey,
            tripId: tripId, paymentId: paymentId, paymentRefundId: paymentRefundId);
        if (txnResult.IsFailure)
        {
            return txnResult.Error;
        }

        var txn = txnResult.Value;
        db.WalletTransactions.Add(txn);

        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            var creditResult = account.Credit(amount);
            if (creditResult.IsFailure)
            {
                return creditResult.Error;
            }

            txn.MarkCommitted(account.Balance);

            try
            {
                await db.SaveChangesAsync(ct);
                return Result.Success;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return Result.Success; // concurrent identical reversal — idempotent
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries - 1)
            {
                await db.Entry(account).ReloadAsync(ct);
                await db.Entry(txn).ReloadAsync(ct);
            }
        }

        logger.LogError("Wallet refund reversal for payment {PaymentId} failed after {Retries} concurrency retries.", paymentId, MaxConcurrencyRetries);
        return WalletErrors.AccountNotFound;
    }

    public async Task<Result<WalletHoldOutcome>> TryHoldForTripAsync(
        Guid userId,
        Guid tripId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        if (amount <= 0m)
        {
            return new WalletHoldOutcome(0m, null);
        }

        var rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

        // Idempotency: this hold already exists — return its reserved amount.
        var existing = await db.WalletTransactions
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null)
        {
            var held = existing.Status is WalletTransactionStatus.Pending or WalletTransactionStatus.Committed
                ? existing.Amount
                : 0m;
            return new WalletHoldOutcome(held, existing.Id);
        }

        var account = await db.WalletAccounts.FirstOrDefaultAsync(a => a.UserId == userId, ct);
        if (account is null)
        {
            return new WalletHoldOutcome(0m, null);
        }

        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            if (account.Balance <= 0m)
            {
                return new WalletHoldOutcome(0m, null);
            }

            var portion = Math.Min(account.Balance, rounded);

            var txnResult = WalletTransaction.CreateTripPaymentHold(
                Guid.NewGuid(), account.Id, portion, currency, idempotencyKey, tripId);
            if (txnResult.IsFailure)
            {
                return txnResult.Error;
            }

            var txn = txnResult.Value;
            var debitResult = account.Debit(portion); // reserve immediately
            if (debitResult.IsFailure)
            {
                return debitResult.Error;
            }

            db.WalletTransactions.Add(txn);

            try
            {
                await db.SaveChangesAsync(ct);
                return new WalletHoldOutcome(portion, txn.Id);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                db.Entry(txn).State = EntityState.Detached;
                await db.Entry(account).ReloadAsync(ct);
                var posted = await db.WalletTransactions
                    .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);
                return new WalletHoldOutcome(posted?.Amount ?? 0m, posted?.Id);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries - 1)
            {
                db.Entry(txn).State = EntityState.Detached;
                await db.Entry(account).ReloadAsync(ct);
            }
        }

        logger.LogError("Wallet trip hold for trip {TripId} failed after {Retries} concurrency retries.", tripId, MaxConcurrencyRetries);
        return WalletErrors.InsufficientBalance;
    }

    public async Task<Result<decimal>> CommitTripHoldAsync(Guid tripId, CancellationToken ct = default)
    {
        var holds = await db.WalletTransactions
            .Where(t => t.TripId == tripId
                && t.Type == WalletTransactionType.TripPayment
                && t.Direction == WalletTransactionDirection.Debit
                && (t.Status == WalletTransactionStatus.Pending || t.Status == WalletTransactionStatus.Committed))
            .ToListAsync(ct);

        if (holds.Count == 0)
        {
            return 0m;
        }

        var account = await db.WalletAccounts.FirstOrDefaultAsync(a => a.Id == holds[0].WalletAccountId, ct);
        var committed = 0m;
        var changed = false;

        foreach (var hold in holds)
        {
            committed += hold.Amount;
            if (hold.Status == WalletTransactionStatus.Pending)
            {
                // Balance was already reduced when the hold was placed — just finalize it.
                hold.MarkCommitted(account?.Balance ?? 0m);
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync(ct);
        }

        return committed;
    }

    public async Task<Result<Success>> ReleaseTripHoldAsync(Guid tripId, CancellationToken ct = default)
    {
        var holds = await db.WalletTransactions
            .Where(t => t.TripId == tripId
                && t.Type == WalletTransactionType.TripPayment
                && t.Direction == WalletTransactionDirection.Debit
                && t.Status == WalletTransactionStatus.Pending)
            .ToListAsync(ct);

        if (holds.Count == 0)
        {
            return Result.Success;
        }

        var account = await db.WalletAccounts.FirstOrDefaultAsync(a => a.Id == holds[0].WalletAccountId, ct);
        if (account is null)
        {
            return WalletErrors.AccountNotFound;
        }

        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            foreach (var hold in holds)
            {
                if (hold.Status != WalletTransactionStatus.Pending)
                {
                    continue;
                }

                account.Credit(hold.Amount); // restore the reserved amount
                hold.MarkReleased();
            }

            try
            {
                await db.SaveChangesAsync(ct);
                return Result.Success;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries - 1)
            {
                await db.Entry(account).ReloadAsync(ct);
                foreach (var hold in holds)
                {
                    await db.Entry(hold).ReloadAsync(ct);
                }
            }
        }

        logger.LogError("Wallet trip hold release for trip {TripId} failed after {Retries} concurrency retries.", tripId, MaxConcurrencyRetries);
        return WalletErrors.AccountNotFound;
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private async Task<Result<WalletAccount>> GetOrCreateAccountAsync(Guid userId, string currency, CancellationToken ct)
    {
        var existing = await db.WalletAccounts.FirstOrDefaultAsync(a => a.UserId == userId, ct);
        if (existing is not null)
        {
            return existing;
        }

        var createResult = WalletAccount.Create(Guid.NewGuid(), userId, currency);
        if (createResult.IsFailure)
        {
            return createResult.Error;
        }

        db.WalletAccounts.Add(createResult.Value);

        try
        {
            await db.SaveChangesAsync(ct);
            return createResult.Value;
        }
        catch (DbUpdateException)
        {
            // Unique(UserId) violation: a concurrent request created the account first.
            db.Entry(createResult.Value).State = EntityState.Detached;
            var reloaded = await db.WalletAccounts.FirstOrDefaultAsync(a => a.UserId == userId, ct);
            return reloaded is not null ? reloaded : WalletErrors.AccountNotFound;
        }
    }

    private static WalletTopUpCreditOutcome NotAWalletTopUp()
        => new(WalletTopUpCreditStatus.NotAWalletTopUp, Guid.Empty, 0m, 0m, string.Empty);
}
