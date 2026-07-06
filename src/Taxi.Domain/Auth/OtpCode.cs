using System.Security.Cryptography;
using System.Text;

using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Auth;

/// <summary>
/// A backend-generated one-time code. The row <see cref="Common.Entity.Id"/> is the
/// public <c>otpRequestId</c> / <c>verificationSessionId</c> returned to the client;
/// verification is keyed by that id plus the code, binding each verification to a
/// single issued session. Only the HMAC hash of the code is stored — never the plain code.
/// </summary>
public sealed class OtpCode : AuditableEntity
{
    private OtpCode()
    {
    }

    private OtpCode(
        Guid id,
        OtpChannel channel,
        OtpPurpose purpose,
        string recipient,
        string codeHash,
        DateTimeOffset nowUtc,
        DateTimeOffset expiresAtUtc,
        int maxAttempts,
        string? ipAddress,
        string? deviceId)
        : base(id)
    {
        Channel = channel;
        Purpose = purpose;
        Recipient = recipient;
        CodeHash = codeHash;
        ExpiresAtUtc = expiresAtUtc;
        MaxAttempts = maxAttempts;
        FailedAttempts = 0;
        ResendCount = 0;
        LastSentAtUtc = nowUtc;
        IpAddress = ipAddress;
        DeviceId = deviceId;
    }

    public OtpChannel Channel { get; private set; }
    public OtpPurpose Purpose { get; private set; }

    /// <summary>Normalized recipient (E.164 phone or lower-cased email).</summary>
    public string Recipient { get; private set; } = default!;

    public string CodeHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }
    public int FailedAttempts { get; private set; }
    public int MaxAttempts { get; private set; }
    public int ResendCount { get; private set; }
    public DateTimeOffset LastSentAtUtc { get; private set; }
    public string? IpAddress { get; private set; }
    public string? DeviceId { get; private set; }

    /// <summary>Provider (CM.com) message id/reference, stored for debugging when available.</summary>
    public string? ProviderMessageId { get; private set; }

    public bool IsConsumed => ConsumedAtUtc is not null;

    public static OtpCode Create(
        OtpChannel channel,
        OtpPurpose purpose,
        string recipient,
        string codeHash,
        DateTimeOffset nowUtc,
        TimeSpan lifetime,
        int maxAttempts,
        string? ipAddress = null,
        string? deviceId = null)
        => new(
            Guid.NewGuid(),
            channel,
            purpose,
            recipient,
            codeHash,
            nowUtc,
            nowUtc.Add(lifetime),
            maxAttempts,
            ipAddress,
            deviceId);

    /// <summary>
    /// Validates a candidate code (already hashed by the caller with the same server
    /// secret) against this row. On success the code is marked consumed (single-use);
    /// on a wrong code the failed-attempt counter is incremented.
    /// </summary>
    public Result<Success> Verify(string providedCodeHash, DateTimeOffset nowUtc)
    {
        if (IsConsumed)
        {
            return OtpErrors.AlreadyUsed;
        }

        if (nowUtc >= ExpiresAtUtc)
        {
            return OtpErrors.Expired;
        }

        if (FailedAttempts >= MaxAttempts)
        {
            return OtpErrors.MaxAttempts;
        }

        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedCodeHash),
            Encoding.UTF8.GetBytes(CodeHash));

        if (!matches)
        {
            FailedAttempts++;
            return FailedAttempts >= MaxAttempts ? OtpErrors.MaxAttempts : OtpErrors.Invalid;
        }

        ConsumedAtUtc = nowUtc;
        return Result.Success;
    }

    public void SetProviderMessageId(string? providerMessageId)
        => ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? null : providerMessageId;
}
