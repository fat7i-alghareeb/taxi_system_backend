using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.PaymentMethods;

public static class PassengerPaymentMethodErrors
{
    public static readonly Error PassengerIdRequired = Error.Validation(
        code: LocalizationKeys.PassengerPaymentMethod.PassengerIdRequired,
        description: "Passenger ID is required.");

    public static readonly Error GatewayPaymentMethodIdRequired = Error.Validation(
        code: LocalizationKeys.PassengerPaymentMethod.GatewayPaymentMethodIdRequired,
        description: "Payment gateway method ID is required.");

    public static readonly Error CardBrandRequired = Error.Validation(
        code: LocalizationKeys.PassengerPaymentMethod.CardBrandRequired,
        description: "Card brand is required.");

    public static readonly Error LastFourInvalid = Error.Validation(
        code: LocalizationKeys.PassengerPaymentMethod.LastFourInvalid,
        description: "Card last four digits are invalid.");

    public static readonly Error ExpiryMonthInvalid = Error.Validation(
        code: LocalizationKeys.PassengerPaymentMethod.ExpiryMonthInvalid,
        description: "Card expiry month is invalid.");

    public static readonly Error ExpiryYearInvalid = Error.Validation(
        code: LocalizationKeys.PassengerPaymentMethod.ExpiryYearInvalid,
        description: "Card expiry year is invalid.");

    public static readonly Error NotFound = Error.NotFound(
        code: LocalizationKeys.PassengerPaymentMethod.NotFound,
        description: "Payment method not found.");

    public static readonly Error NotReusableCard = Error.Validation(
        code: LocalizationKeys.PassengerPaymentMethod.NotReusableCard,
        description: "Only reusable cards can be saved for future charges.");

    public static readonly Error PreferredMethodInvalid = Error.Validation(
        code: LocalizationKeys.PassengerPaymentMethod.PreferredMethodInvalid,
        description: "The selected payment method is not available.");
}
