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
}
