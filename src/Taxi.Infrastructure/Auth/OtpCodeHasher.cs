using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;

namespace Taxi.Infrastructure.Auth;

/// <summary>
/// HMAC-SHA256 hasher keyed by <see cref="OtpOptions.HashSecret"/>. Deterministic so a
/// candidate code can be re-hashed and compared against the stored hash — the plain code
/// is never persisted.
/// </summary>
internal sealed class OtpCodeHasher : IOtpCodeHasher
{
    private readonly byte[] _key;

    public OtpCodeHasher(IOptions<OtpOptions> options)
    {
        var secret = options.Value.HashSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "Otp:HashSecret is missing. Provide it via user-secrets / environment variables.");
        }

        _key = Encoding.UTF8.GetBytes(secret);
    }

    public string Hash(string code)
    {
        var hash = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(code));
        return Convert.ToHexStringLower(hash);
    }
}
