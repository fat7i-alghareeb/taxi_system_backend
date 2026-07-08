namespace Taxi.Contracts.Requests.PaymentMethods;

/// <summary>Body for setting the preferred trip-booking method type (null clears the preference).</summary>
public class SetPaymentPreferenceRequest
{
    public string? PreferredMethodType { get; set; }
}
