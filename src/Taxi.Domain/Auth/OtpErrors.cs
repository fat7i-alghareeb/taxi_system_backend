using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Auth;

public static class OtpErrors
{
    public static readonly Error NotFound = Error.NotFound(
        code: LocalizationKeys.Otp.NotFound,
        description: "The verification request was not found. Please request a new code.");

    public static readonly Error Expired = Error.Validation(
        code: LocalizationKeys.Otp.Expired,
        description: "The verification code has expired. Please request a new code.");

    public static readonly Error Invalid = Error.Validation(
        code: LocalizationKeys.Otp.Invalid,
        description: "The verification code is incorrect.");

    public static readonly Error MaxAttempts = Error.Validation(
        code: LocalizationKeys.Otp.MaxAttempts,
        description: "Too many attempts. Please request a new code.");

    public static readonly Error AlreadyUsed = Error.Validation(
        code: LocalizationKeys.Otp.AlreadyUsed,
        description: "This verification code has already been used.");

    public static readonly Error ResendCooldown = Error.Validation(
        code: LocalizationKeys.Otp.ResendCooldown,
        description: "Please wait before requesting another code.");

    public static readonly Error SendFailed = Error.Failure(
        code: LocalizationKeys.Otp.SendFailed,
        description: "We could not send the verification code. Please try again.");

    public static readonly Error ChannelMismatch = Error.Validation(
        code: LocalizationKeys.Otp.ChannelMismatch,
        description: "The verification request does not match this channel.");
}
