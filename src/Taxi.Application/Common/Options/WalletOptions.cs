namespace Taxi.Application.Common.Options;

/// <summary>
/// Wallet (ride balance) policy. Bound from the <c>Wallet</c> section in appsettings.
/// The min/max top-up bounds are BUSINESS CONFIG placeholders — confirm the real values
/// with the business before go-live; they are intentionally not hardcoded in logic.
/// </summary>
public sealed class WalletOptions
{
    public const string SectionName = "Wallet";

    /// <summary>ISO 4217 currency for the wallet (single-currency platform).</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>Smallest allowed single top-up, in major units.</summary>
    public decimal MinTopUpAmount { get; set; } = 5m;

    /// <summary>Largest allowed single top-up, in major units.</summary>
    public decimal MaxTopUpAmount { get; set; } = 500m;
}
