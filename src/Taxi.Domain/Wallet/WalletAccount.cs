using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Wallet;

/// <summary>
/// A passenger's in-app ride balance ("Fat7i Saldo"). One account per passenger.
/// The authoritative money source is the <see cref="WalletTransaction"/> ledger; this
/// entity carries a materialized <see cref="Balance"/> (== sum of committed ledger entries)
/// for fast reads. Balance mutations are guarded by an optimistic concurrency token
/// (<see cref="Balance"/> itself, configured in EF) so concurrent credits never lose a write.
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

    /// <summary>
    /// Materialized balance in major units (decimal). Normally zero or positive, but goes
    /// NEGATIVE when a fee could not be collected — see <see cref="ChargeUncollectableFee"/>.
    /// A negative balance is the customer's debt to the platform.
    /// </summary>
    public decimal Balance { get; private set; }

    /// <summary>True when the customer owes the platform money.</summary>
    public bool IsInDebt => Balance < 0m;

    /// <summary>The debt as a positive amount, or zero when the balance is not negative.</summary>
    public decimal AmountOwed => Balance < 0m ? -Balance : 0m;

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
    /// Decreases the balance, refusing to overdraw. This is what stops a customer paying for a
    /// ride they cannot afford — trip payments and holds all go through here. Callers pair this
    /// with a committed debit ledger entry.
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

    /// <summary>
    /// Charges a fee that has already been incurred and could not be collected any other way,
    /// taking the balance NEGATIVE if needed. The only method in the system that may overdraw.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from <see cref="Debit"/>: a fee like waiting time is incurred by the
    /// customer and cannot be declined, so refusing it would only lose the money — previously an
    /// uncollectable waiting fee vanished with no record at all. A ride, by contrast, is optional
    /// and must never be taken on credit, which is why <see cref="Debit"/> keeps refusing to
    /// overdraw. The resulting debt blocks further booking until it is settled.
    /// </remarks>
    public Result<Success> ChargeUncollectableFee(decimal amount)
    {
        if (amount <= 0m)
        {
            return WalletErrors.InvalidAmount;
        }

        Balance = decimal.Round(Balance - amount, 2, MidpointRounding.AwayFromZero);
        return Result.Success;
    }
}
