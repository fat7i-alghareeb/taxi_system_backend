namespace Taxi.Application.Features.PaymentMethods.Dtos;

public record PaymentMethodDto(
    Guid Id,
    string CardBrand,
    string LastFour,
    int ExpiryMonth,
    int ExpiryYear,
    string? CardholderName,
    bool IsDefault);
