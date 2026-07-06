using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Users;

// Auth-flow errors (Firebase, identity, phone-claim mismatch).
// User-property errors live in UserErrors.
public static class AuthErrors
{
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

    public static readonly Error NoAccountFound = Error.NotFound(
        code: LocalizationKeys.Auth.NoAccountFound,
        description: "No account found. Please create an account first.");

    public static readonly Error AccountAlreadyExists = Error.Conflict(
        code: LocalizationKeys.Auth.AccountAlreadyExists,
        description: "You already have an account.");

    public static readonly Error EmailAlreadyRegistered = Error.Conflict(
        code: LocalizationKeys.Auth.EmailAlreadyRegistered,
        description: "You already have an account.");

    public static readonly Error PhoneAlreadyVerifiedElsewhere = Error.Conflict(
        code: LocalizationKeys.Auth.PhoneAlreadyVerifiedElsewhere,
        description: "This phone number is already verified on another account.");

    public static readonly Error RegistrationTokenInvalid = Error.Unauthorized(
        code: LocalizationKeys.Auth.RegistrationTokenInvalid,
        description: "The registration session is invalid or has expired. Please start again.");

    public static readonly Error InvalidGoogleToken = Error.Unauthorized(
        code: LocalizationKeys.Auth.InvalidGoogleToken,
        description: "The Google sign-in could not be verified.");

    public static readonly Error GoogleEmailMissing = Error.Validation(
        code: LocalizationKeys.Auth.GoogleEmailMissing,
        description: "The Google account did not provide an email address.");
}
