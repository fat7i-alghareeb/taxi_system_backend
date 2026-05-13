using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Users;

public static class AuthErrors
{
    public static readonly Error NameEnRequired = Error.Validation(
        code: LocalizationKeys.User.NameEnRequired,
        description: "English name is required.");

    public static readonly Error NameArRequired = Error.Validation(
        code: LocalizationKeys.User.NameArRequired,
        description: "Arabic name is required.");

    public static readonly Error NameNlRequired = Error.Validation(
        code: LocalizationKeys.User.NameNlRequired,
        description: "Dutch name is required.");

    public static readonly Error NameDeRequired = Error.Validation(
        code: LocalizationKeys.User.NameDeRequired,
        description: "German name is required.");

    public static readonly Error NamePlRequired = Error.Validation(
        code: LocalizationKeys.User.NamePlRequired,
        description: "Polish name is required.");

    public static readonly Error NameUkRequired = Error.Validation(
        code: LocalizationKeys.User.NameUkRequired,
        description: "Ukrainian name is required.");

    public static readonly Error NameFrRequired = Error.Validation(
        code: LocalizationKeys.User.NameFrRequired,
        description: "French name is required.");

    public static readonly Error NameEsRequired = Error.Validation(
        code: LocalizationKeys.User.NameEsRequired,
        description: "Spanish name is required.");

    public static readonly Error NameRoRequired = Error.Validation(
        code: LocalizationKeys.User.NameRoRequired,
        description: "Romanian name is required.");

    public static readonly Error PhoneRequired = Error.Validation(
        code: LocalizationKeys.User.PhoneRequired,
        description: "Phone number is required.");

    public static readonly Error UserInactive = Error.Forbidden(
        code: LocalizationKeys.User.Inactive,
        description: "User account is inactive.");

    public static readonly Error UserNotFound = Error.NotFound(
        code: LocalizationKeys.User.NotFound,
        description: "User not found.");

    public static readonly Error InvalidFirebaseToken = Error.Unauthorized(
        code: LocalizationKeys.Auth.InvalidFirebaseToken,
        description: "The Firebase token is invalid.");

    public static readonly Error FirebaseTokenExpired = Error.Unauthorized(
        code: LocalizationKeys.Auth.FirebaseTokenExpired,
        description: "The Firebase token has expired. Please sign in again.");

    public static readonly Error FirebasePhoneMissing = Error.Validation(
        code: LocalizationKeys.Auth.FirebasePhoneMissing,
        description: "The Firebase token does not include a phone number.");

    public static readonly Error PhoneMismatch = Error.Validation(
        code: LocalizationKeys.Auth.PhoneMismatch,
        description: "The phone number does not match the verified Firebase identity.");
}

