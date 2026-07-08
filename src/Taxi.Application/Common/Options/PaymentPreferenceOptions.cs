namespace Taxi.Application.Common.Options;

/// <summary>
/// Payment configuration bound from the <c>Payments</c> section. <see cref="EnabledMethodTypes"/>
/// is the set of on-session method types offered as a preferred trip-booking method; it should
/// mirror what is enabled in the Stripe Dashboard.
/// </summary>
public sealed class PaymentPreferenceOptions
{
    public const string SectionName = "Payments";

    public string[] EnabledMethodTypes { get; set; } =
        ["card", "apple_pay", "google_pay", "ideal", "klarna", "paypal"];
}
