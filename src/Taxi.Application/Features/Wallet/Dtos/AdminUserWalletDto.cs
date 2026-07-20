namespace Taxi.Application.Features.Wallet.Dtos;

/// <summary>
/// Admin view of a passenger's wallet: current balance, any debt, plus the most recent ledger
/// entries. <paramref name="AmountOwed"/> matters because a debt no longer shows up as an unpaid
/// trip — the fee is settled against the wallet, so the receivable is only visible here.
/// Adjustments are made through the separate wallet-adjustments endpoint.
/// </summary>
public record AdminUserWalletDto(
    Guid UserId,
    bool HasAccount,
    decimal Balance,
    string CurrencyCode,
    IReadOnlyList<WalletTransactionDto> Transactions,
    decimal AmountOwed = 0m);
