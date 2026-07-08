namespace Taxi.Application.Features.Wallet.Dtos;

public record WalletTransactionDto(
    Guid Id,
    string Type,
    string Direction,
    decimal Amount,
    string CurrencyCode,
    decimal? BalanceAfter,
    string Status,
    string? Description,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);
