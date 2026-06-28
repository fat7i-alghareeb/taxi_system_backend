namespace Taxi.Contracts.Requests.Config;

/// <summary>VAT/BTW rate as a fraction (e.g. 0.09 = 9%).</summary>
public record UpdateVatRateRequest(decimal Rate);
