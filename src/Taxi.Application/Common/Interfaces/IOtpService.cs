using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

/// <summary>Result of issuing a code: the public request id and client-facing timings.</summary>
public sealed record OtpIssueResult(Guid OtpRequestId, int ExpiresInSeconds, int ResendAvailableInSeconds);

/// <summary>
/// Backend-owned OTP orchestration: generation, hashing, storage, expiry, resend
/// cooldown, attempt limits and delivery via <see cref="ISmsSender"/>/<see cref="IEmailSender"/>.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generates a code, stores its hash, sends it over the channel and returns the
    /// request id + timings. Enforces the resend cooldown per recipient/purpose.
    /// </summary>
    Task<Result<OtpIssueResult>> IssueAsync(
        OtpChannel channel,
        OtpPurpose purpose,
        string recipient,
        string? ipAddress,
        string? deviceId,
        CancellationToken ct = default);

    /// <summary>
    /// Verifies a code against a specific issued request. On success marks it consumed
    /// and returns the verified recipient (normalized phone/email).
    /// </summary>
    Task<Result<string>> VerifyAsync(
        Guid otpRequestId,
        string code,
        OtpChannel expectedChannel,
        OtpPurpose expectedPurpose,
        CancellationToken ct = default);
}
