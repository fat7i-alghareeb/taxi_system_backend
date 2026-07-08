namespace Taxi.Contracts.Requests.Wallet;

/// <summary>Body for starting a wallet top-up. Amount bounds are validated server-side.</summary>
public class CreateWalletTopUpRequest
{
    public decimal Amount { get; set; }
}
