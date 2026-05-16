using Taxi.Contracts.Common;

namespace Taxi.Infrastructure.Settings;

public class AppSettings
{
    public string CorsPolicyName { get; set; } = default!;

    public string[] AllowedOrigins { get; set; } = default!;

    /// <summary>
    /// Gets or sets the default language used when the Accept-Language header is absent or unsupported.
    /// Supported values: "en" (English), "ar" (Arabic). Defaults to "en".
    /// </summary>
    public string DefaultLanguage { get; set; } = Languages.Default;

    public string GoogleMapsApiKey { get; set; } = default!;

    public string FirebaseCredentials { get; set; } = default!;

    public StripeSettings Stripe { get; set; } = new();

    public FeatureFlags Features { get; set; } = new();
}

public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;

    public string PublishableKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public bool TestMode { get; set; } = true;
}

public class FeatureFlags
{
    public bool StripeEnabled { get; set; }

    public bool SignalREnabled { get; set; } = true;
}
