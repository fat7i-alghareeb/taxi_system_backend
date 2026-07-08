using Taxi.Application.Features.Trips.Dtos;

namespace Taxi.Application.Features.Wallet.Dtos;

/// <summary>
/// Result of starting a wallet top-up: the amount/currency plus the Stripe payment-sheet
/// details the app opens. The wallet is credited only after the payment succeeds (webhook).
/// </summary>
public record WalletTopUpDto(
    decimal Amount,
    string CurrencyCode,
    StripePaymentDto StripePayment);
