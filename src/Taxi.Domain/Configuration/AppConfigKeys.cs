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

    // Remote version gate for the customer app (Fat7i, dev.fat7i.customertaxi).
    // The master switch is stored as "true"/"false" because the AppConfig
    // invariant forbids empty values; a missing or unparseable value reads as
    // false, so a lost row disables the gate rather than locking users out.
    // The per-platform values are blank when unconfigured, which the client
    // treats as "do not gate this platform".
    public const string AppUpdateCheckEnabled = "AppUpdateCheckEnabled";
    public const string AndroidLatestVersion = "AndroidLatestVersion";
    public const string AndroidMinimumRequiredVersion = "AndroidMinimumRequiredVersion";
    public const string AndroidStoreUrl = "AndroidStoreUrl";
    public const string IosLatestVersion = "IosLatestVersion";
    public const string IosMinimumRequiredVersion = "IosMinimumRequiredVersion";
    public const string IosStoreUrl = "IosStoreUrl";
}
