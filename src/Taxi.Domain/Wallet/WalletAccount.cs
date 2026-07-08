using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Wallet;

/// <summary>
/// A passenger's in-app ride balance ("Fat7i Saldo"). One account per passenger.
/// The authoritative money source is the <see cref="WalletTransaction"/> ledger; this
/// entity carries a materialized <see cref="Balance"/> (== sum of committed ledger entries)
/// for fast reads. Balance mutations are guarded by an optimistic concurrency token
/// (Postgres xmin, configured in EF) so concurrent credits never lose a write.
/// </summary>
public sealed class WalletAccount : AuditableEntity
{
    private WalletAccount() { } // EF Core

    private WalletAccount(Guid id, Guid userId, string currency)
        : base(id)
    {
        UserId = userId;
        Currency = currency;
        Balance = 0m;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }

    /// <summary>ISO 4217 currency code (single-currency platform, e.g. "EUR").</summary>
    public string Currency { get; private set; } = default!;

    /// <summary>Materialized balance in major units (decimal). Never negative in Phase 1 (credit-only).</summary>
    public decimal Balance { get; private set; }

    public static Result<WalletAccount> Create(Guid id, Guid userId, string currency)
    {
        if (userId == Guid.Empty)
        {
            return WalletErrors.AccountNotFound;
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return WalletErrors.CurrencyRequired;
        }

        return new WalletAccount(id, userId, currency.Trim().ToUpperInvariant());
    }

    /// <summary>
    /// Increases the balance. Callers pair this with a committed <see cref="WalletTransaction"/>
    /// in the same unit of work so the ledger and materialized balance stay consistent.
    /// </summary>
    public Result<Success> Credit(decimal amount)
    {
        if (amount <= 0m)
        {
            return WalletErrors.InvalidAmount;
        }

        Balance = decimal.Round(Balance + amount, 2, MidpointRounding.AwayFromZero);
        return Result.Success;
    }

    /// <summary>
    /// Decreases the balance. The amount must be positive and no more than the current balance —
    /// wallet balances never go negative. Callers pair this with a committed debit ledger entry.
    /// </summary>
    public Result<Success> Debit(decimal amount)
    {
        if (amount <= 0m)
        {
            return WalletErrors.InvalidAmount;
        }

        if (amount > Balance)
        {
            return WalletErrors.InsufficientBalance;
        }

        Balance = decimal.Round(Balance - amount, 2, MidpointRounding.AwayFromZero);
        return Result.Success;
    }
}
