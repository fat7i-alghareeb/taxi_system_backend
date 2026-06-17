using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Users;

public static class UserErrors
{
    public static readonly Error NameRequired = Error.Validation(
        code: LocalizationKeys.User.NameRequired,
        description: "Name is required.");

    public static readonly Error PhoneRequired = Error.Validation(
        code: LocalizationKeys.User.PhoneRequired,
        description: "Phone number is required.");

    public static readonly Error ProfileNameRequired = Error.Validation(
        code: LocalizationKeys.User.ProfileNameRequired,
        description: "Profile name is required.");

    public static readonly Error FcmTokenInvalid = Error.Validation(
        code: LocalizationKeys.User.FcmTokenInvalid,
        description: "FCM token is invalid.");

    public static readonly Error PreferredLanguageRequired = Error.Validation(
        code: LocalizationKeys.User.PreferredLanguageRequired,
        description: "Preferred language is required.");

    public static readonly Error PreferredLanguageInvalid = Error.Validation(
        code: LocalizationKeys.User.PreferredLanguageInvalid,
        description: "Preferred language is invalid.");

    public static readonly Error StripeCustomerIdRequired = Error.Validation(
        code: LocalizationKeys.User.StripeCustomerIdRequired,
        description: "Stripe customer id is required.");

    public static readonly Error HomeAddressInvalid = Error.Validation(
        code: LocalizationKeys.User.HomeAddressInvalid,
        description: "Home address is invalid.");

    public static readonly Error Inactive = Error.Forbidden(
        code: LocalizationKeys.User.Inactive,
        description: "User account is inactive.");

    public static readonly Error NotFound = Error.NotFound(
        code: LocalizationKeys.User.NotFound,
        description: "User not found.");

    public static readonly Error NotADriver = Error.Validation(
        code: LocalizationKeys.User.NotADriver,
        description: "User is not a driver.");
}
