namespace Taxi.Application.Common.Options;

/// <summary>
/// Issuer details printed on every invoice. Bound from <c>Invoice:Issuer</c>
/// in appsettings.
/// </summary>
public sealed class InvoiceIssuerOptions
{
    public const string SectionName = "Invoice:Issuer";

    public string Name { get; set; } = "Fat7i";
    public string Address { get; set; } = string.Empty;
    public string? VatNumber { get; set; }
}
