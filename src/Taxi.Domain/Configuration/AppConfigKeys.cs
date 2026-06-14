namespace Taxi.Domain.Configuration;

public static class AppConfigKeys
{
    public const string TripDiscountPercent = "TripDiscountPercent";
    public const string Currency = "Currency";

    // Admin-editable company contact details printed in the invoice footer.
    public const string CompanyEmail = "CompanyEmail";
    public const string CompanyPhone = "CompanyPhone";
    public const string CompanyWebsite = "CompanyWebsite";
}
