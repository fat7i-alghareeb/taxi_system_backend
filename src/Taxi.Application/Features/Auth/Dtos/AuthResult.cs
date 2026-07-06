namespace Taxi.Application.Features.Auth.Dtos;

/// <summary>
/// Verified email/Google identity that still needs a phone before a full account exists.
/// </summary>
public sealed record RegistrationChallenge(string RegistrationToken, string? Email, string? Name);

/// <summary>
/// Result of a Google / email-signup verification: either an existing account
/// <see cref="Session"/>, or a <see cref="Registration"/> challenge for a brand-new account
/// (client collects name + phone, then calls register/complete).
/// </summary>
public sealed record AuthResult(AuthResponse? Session, RegistrationChallenge? Registration)
{
    public bool RequiresRegistration => Registration is not null;

    public static AuthResult ForSession(AuthResponse session) => new(session, null);

    public static AuthResult ForRegistration(RegistrationChallenge challenge) => new(null, challenge);
}
