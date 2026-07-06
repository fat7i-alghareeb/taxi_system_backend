using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

/// <summary>Identity extracted from a verified Firebase ID token (used for Google sign-in).</summary>
public sealed record FirebaseIdentity(
    string Uid,
    string? Email,
    bool EmailVerified,
    string? Name,
    string? Phone,
    string? SignInProvider);

public interface IFirebaseAuthService
{
    /// <summary>Legacy phone-token verification (kept for the deprecated Firebase phone login).</summary>
    Task<Result<string>> VerifyIdTokenAndGetPhoneAsync(string idToken, CancellationToken ct = default);

    /// <summary>
    /// Verifies a Firebase ID token (Google sign-in) and returns the identity claims.
    /// </summary>
    Task<Result<FirebaseIdentity>> VerifyIdTokenAndGetIdentityAsync(string idToken, CancellationToken ct = default);
}
