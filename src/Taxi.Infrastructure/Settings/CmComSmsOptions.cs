namespace Taxi.Infrastructure.Settings;

/// <summary>
/// CM.com Business Messaging / SMS API configuration. Bound from <c>Sms:CmCom</c>.
/// Used only to send plain SMS (e.g. phone OTP). The product token must never be
/// exposed to clients — provide it via user-secrets / environment variables.
/// </summary>
public sealed class CmComSmsOptions
{
    public const string SectionName = "Sms:CmCom";

    /// <summary>Business Messaging API base URL (global Cloudflare gateway by default).</summary>
    public string BaseUrl { get; set; } = "https://gw.messaging.cm.com/";

    /// <summary>CM.com product (gateway) token used to authenticate requests.</summary>
    public string ProductToken { get; set; } = string.Empty;

    /// <summary>Configurable sender name/originator shown to the recipient.</summary>
    public string Sender { get; set; } = "Fat7i";
}
