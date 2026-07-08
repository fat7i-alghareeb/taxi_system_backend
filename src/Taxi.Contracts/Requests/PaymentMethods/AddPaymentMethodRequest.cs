namespace Taxi.Contracts.Requests.PaymentMethods;

/// <summary>Body for persisting a reusable payment method after a confirmed SetupIntent.</summary>
public class AddPaymentMethodRequest
{
    public string PaymentMethodId { get; set; } = string.Empty;

    public bool SetAsDefault { get; set; }
}
