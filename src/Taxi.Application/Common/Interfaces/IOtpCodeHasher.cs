namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Deterministically hashes OTP codes (HMAC with a server secret) so only the hash
/// is ever stored/compared — the plain code never touches the database or logs.
/// </summary>
public interface IOtpCodeHasher
{
    string Hash(string code);
}
