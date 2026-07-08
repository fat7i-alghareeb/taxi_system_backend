namespace Taxi.Application.Features.Wallet.Dtos;

public record WalletBalanceDto(
    decimal Balance,
    string CurrencyCode);
