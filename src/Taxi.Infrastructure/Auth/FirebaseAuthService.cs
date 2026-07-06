using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Infrastructure.Auth;

internal sealed class FirebaseAuthService(ILogger<FirebaseAuthService> logger) : IFirebaseAuthService
{
    public async Task<Result<string>> VerifyIdTokenAndGetPhoneAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var decoded = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(idToken, checkRevoked: true, ct);

            if (!decoded.Claims.TryGetValue("phone_number", out var phoneObj)
                || phoneObj is not string phone
                || string.IsNullOrWhiteSpace(phone))
            {
                logger.LogWarning("Firebase ID token has no phone_number claim. UID={Uid}", decoded.Uid);
                return AuthErrors.FirebasePhoneMissing;
            }

            return phone;
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.ExpiredIdToken)
        {
            return AuthErrors.FirebaseTokenExpired;
        }
        catch (FirebaseAuthException ex)
        {
            logger.LogWarning(ex, "Firebase ID token verification failed: {Code}", ex.AuthErrorCode);
            return AuthErrors.InvalidFirebaseToken;
        }
    }

    public async Task<Result<FirebaseIdentity>> VerifyIdTokenAndGetIdentityAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var decoded = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(idToken, checkRevoked: true, ct);

            string? email = GetClaim(decoded.Claims, "email");
            var emailVerified = decoded.Claims.TryGetValue("email_verified", out var ev) && ev is bool b && b;
            string? name = GetClaim(decoded.Claims, "name");
            string? phone = GetClaim(decoded.Claims, "phone_number");
            string? provider = null;

            if (decoded.Claims.TryGetValue("firebase", out var firebaseObj)
                && firebaseObj is IDictionary<string, object> firebase
                && firebase.TryGetValue("sign_in_provider", out var providerObj))
            {
                provider = providerObj as string;
            }

            return new FirebaseIdentity(decoded.Uid, email, emailVerified, name, phone, provider);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.ExpiredIdToken)
        {
            return AuthErrors.FirebaseTokenExpired;
        }
        catch (FirebaseAuthException ex)
        {
            logger.LogWarning(ex, "Firebase Google token verification failed: {Code}", ex.AuthErrorCode);
            return AuthErrors.InvalidGoogleToken;
        }
    }

    private static string? GetClaim(IReadOnlyDictionary<string, object> claims, string key)
        => claims.TryGetValue(key, out var value) && value is string s && !string.IsNullOrWhiteSpace(s) ? s : null;
}
