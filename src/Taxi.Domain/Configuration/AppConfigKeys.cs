namespace Taxi.Domain.Configuration;

public static class AppConfigKeys
{
    public const string TripDiscountPercent = "TripDiscountPercent";
    public const string Currency = "Currency";

    // VAT/BTW rate applied to invoices, stored as a fraction (e.g. "0.09" = 9%).
    // Prices charged are VAT-inclusive; this rate is used to break the gross
    // amount into net + tax on the invoice.
    public const string VatRate = "VatRate";

    // Admin-editable company contact details printed in the invoice footer.
    public const string CompanyEmail = "CompanyEmail";
    public const string CompanyPhone = "CompanyPhone";
    public const string CompanyWebsite = "CompanyWebsite";

    // Admin-editable support contact used by the in-trip "Report problem" action.
    public const string SupportWhatsApp = "SupportWhatsApp";
}
