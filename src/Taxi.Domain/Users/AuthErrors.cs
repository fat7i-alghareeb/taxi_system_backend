using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Users;

public static class AuthErrors
{
    public static readonly Error NameEnRequired = Error.Validation(
        code: "User.NameEnRequired",
        description: "English name is required.");

    public static readonly Error NameArRequired = Error.Validation(
        code: "User.NameArRequired",
        description: "Arabic name is required.");

    public static readonly Error NameNlRequired = Error.Validation(
        code: "User.NameNlRequired",
        description: "Dutch name is required.");

    public static readonly Error PhoneRequired = Error.Validation(
        code: "User.PhoneRequired",
        description: "Phone number is required.");

    public static readonly Error UserInactive = Error.Forbidden(
        code: "User.Inactive",
        description: "User account is inactive.");

    public static readonly Error UserNotFound = Error.NotFound(
        code: "User.NotFound",
        description: "User not found.");

    public static readonly Error InvalidOtp = Error.Validation(
        code: "Auth.InvalidOtp",
        description: "The provided OTP is incorrect.");

    public static readonly Error OtpExpired = Error.Validation(
        code: "Auth.OtpExpired",
        description: "The OTP has expired.");
}

