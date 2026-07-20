namespace Taxi.Application.Features.Wallet.Dtos;

/// <summary>
/// The customer's balance. <paramref name="Balance"/> is negative when they owe money, and
/// <paramref name="AmountOwed"/> restates that as a positive figure so clients can render "you
/// owe X" without sign-juggling. <paramref name="IsBookingBlocked"/> is the server's own verdict
/// — clients must not re-derive it from the balance, since debts too small to charge don't block.
/// </summary>
public record WalletBalanceDto(
    decimal Balance,
    string CurrencyCode,
    decimal AmountOwed = 0m,
    bool IsBookingBlocked = false);
