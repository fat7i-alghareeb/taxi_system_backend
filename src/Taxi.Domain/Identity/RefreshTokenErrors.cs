using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Identity;

public static class RefreshTokenErrors
{
    public static Error IdRequired => Error.Validation(
        code: "RefreshToken.IdRequired",
        description: "Refresh token id is required");

    public static Error TokenRequired => Error.Validation(
        code: "RefreshToken.TokenRequired",
        description: "Refresh token value is required");

    public static Error UserIdRequired => Error.Validation(
        code: "RefreshToken.UserIdRequired",
        description: "User id is required for refresh token");

    public static Error ExpiryInvalid => Error.Validation(
        code: "RefreshToken.ExpiryInvalid",
        description: "Refresh token expiry date must be in the future");
}

