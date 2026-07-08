using Taxi.Domain.PaymentMethods;

namespace Taxi.Application.Features.PaymentMethods.Dtos;

public static class PaymentMethodMapper
{
    public static PaymentMethodDto ToDto(this PassengerPaymentMethod method)
        => new(
            method.Id,
            method.CardBrand,
            method.LastFour,
            method.ExpiryMonth,
            method.ExpiryYear,
            method.CardholderName,
            method.IsDefault);
}
