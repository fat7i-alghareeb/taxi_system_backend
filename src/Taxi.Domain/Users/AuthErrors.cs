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
}
