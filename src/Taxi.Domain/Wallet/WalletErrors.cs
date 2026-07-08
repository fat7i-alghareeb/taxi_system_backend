using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Wallet;

public static class WalletErrors
{
    public static readonly Error InvalidAmount = Error.Validation(
        code: LocalizationKeys.Wallet.InvalidAmount,
        description: "Amount must be greater than zero.");

    public static readonly Error AccountNotFound = Error.NotFound(
        code: LocalizationKeys.Wallet.AccountNotFound,
        description: "Wallet account not found.");

    public static readonly Error InsufficientBalance = Error.Validation(
        code: LocalizationKeys.Wallet.InsufficientBalance,
        description: "Insufficient wallet balance.");

    public static readonly Error CurrencyRequired = Error.Validation(
        code: LocalizationKeys.Wallet.CurrencyRequired,
        description: "Wallet currency is required.");

    public static readonly Error IdempotencyKeyRequired = Error.Validation(
        code: LocalizationKeys.Wallet.IdempotencyKeyRequired,
        description: "A wallet transaction idempotency key is required.");

    public static readonly Error TopUpDisabled = Error.Validation(
        code: LocalizationKeys.Wallet.TopUpDisabled,
        description: "Wallet top-up is currently unavailable.");

    public static Error TopUpBelowMinimum(decimal minimum, string currency) => Error.Validation(
        code: LocalizationKeys.Wallet.TopUpBelowMinimum,
        description: $"Top-up amount is below the minimum of {minimum:0.00} {currency}.",
        minimum,
        currency);

    public static Error TopUpAboveMaximum(decimal maximum, string currency) => Error.Validation(
        code: LocalizationKeys.Wallet.TopUpAboveMaximum,
        description: $"Top-up amount is above the maximum of {maximum:0.00} {currency}.",
        maximum,
        currency);
}
