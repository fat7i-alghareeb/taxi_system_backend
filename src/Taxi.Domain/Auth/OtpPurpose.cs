namespace Taxi.Domain.Auth;

/// <summary>
/// Why a code was issued. Verification is bound to a specific purpose so a code
/// issued for one flow can never be replayed against another.
/// </summary>
public enum OtpPurpose
{
    PhoneLogin,
    PhoneSignup,
    EmailLogin,
    EmailSignup,

    /// <summary>Verifying/adding a phone on an already-authenticated account.</summary>
    PhoneVerify,
}
