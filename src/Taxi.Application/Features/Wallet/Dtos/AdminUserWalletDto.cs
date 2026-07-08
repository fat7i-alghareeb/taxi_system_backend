namespace Taxi.Application.Features.Wallet.Dtos;

/// <summary>
/// Admin read-only view of a passenger's wallet: current balance plus the most
/// recent ledger entries. View-only — no manual balance edits are exposed.
/// </summary>
public record AdminUserWalletDto(
    Guid UserId,
    bool HasAccount,
    decimal Balance,
    string CurrencyCode,
    IReadOnlyList<WalletTransactionDto> Transactions);
