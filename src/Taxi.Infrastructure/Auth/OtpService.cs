using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;

namespace Taxi.Infrastructure.Auth;

/// <summary>
/// Backend-owned OTP orchestration: secure code generation, hashing, storage, expiry,
/// resend cooldown, attempt limits and delivery. Providers (CM.com / Titan) are dumb pipes
/// behind <see cref="ISmsSender"/>/<see cref="IEmailSender"/>; no OTP-as-a-service is used.
/// </summary>
internal sealed class OtpService(
    IAppDbContext dbContext,
    IOtpCodeHasher hasher,
    ISmsSender smsSender,
    IEmailSender emailSender,
    IOptions<OtpOptions> options,
    TimeProvider timeProvider,
    ILogger<OtpService> logger) : IOtpService
{
    private readonly OtpOptions _options = options.Value;

    public async Task<Result<OtpIssueResult>> IssueAsync(
        OtpChannel channel,
        OtpPurpose purpose,
        string recipient,
        string? ipAddress,
        string? deviceId,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();

        // Resend cooldown per recipient/purpose (defence in depth alongside API rate limiting).
        var cooldown = TimeSpan.FromSeconds(_options.ResendCooldownSeconds);
        var lastSentAt = await dbContext.OtpCodes
            .Where(o => o.Recipient == recipient && o.Channel == channel && o.Purpose == purpose)
            .OrderByDescending(o => o.LastSentAtUtc)
            .Select(o => (DateTimeOffset?)o.LastSentAtUtc)
            .FirstOrDefaultAsync(ct);

        if (lastSentAt is not null && lastSentAt.Value.Add(cooldown) > now)
        {
            return OtpErrors.ResendCooldown;
        }

        var code = GenerateCode(_options.CodeLength);
        var codeHash = hasher.Hash(code);

        var otp = OtpCode.Create(
            channel,
            purpose,
            recipient,
            codeHash,
            now,
            TimeSpan.FromMinutes(_options.ExpiryMinutes),
            _options.MaxAttempts,
            ipAddress,
            deviceId);

        // Deliver first; only persist a row we actually managed to send, so we never
        // leave an undeliverable code behind. The plain code is never logged.
        Result<string> sendResult = channel switch
        {
            OtpChannel.Sms => await smsSender.SendAsync(recipient, BuildSmsBody(code), ct),
            OtpChannel.Email => await SendEmailAsync(recipient, code, ct),
            _ => OtpErrors.SendFailed,
        };

        if (sendResult.IsError)
        {
            return sendResult.Errors;
        }

        if (channel == OtpChannel.Sms)
        {
            otp.SetProviderMessageId(sendResult.Value);
        }

        // Best-effort purge of the recipient's old expired/consumed codes for this channel.
        await PurgeStaleForRecipientAsync(channel, recipient, now, ct);

        dbContext.OtpCodes.Add(otp);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "Issued {Channel} OTP for purpose {Purpose}. RequestId={RequestId}",
            channel,
            purpose,
            otp.Id);

        return new OtpIssueResult(otp.Id, _options.ExpiryMinutes * 60, _options.ResendCooldownSeconds);
    }

    public async Task<Result<string>> VerifyAsync(
        Guid otpRequestId,
        string code,
        OtpChannel expectedChannel,
        OtpPurpose expectedPurpose,
        CancellationToken ct = default)
    {
        var otp = await dbContext.OtpCodes.FirstOrDefaultAsync(o => o.Id == otpRequestId, ct);
        if (otp is null)
        {
            return OtpErrors.NotFound;
        }

        if (otp.Channel != expectedChannel || otp.Purpose != expectedPurpose)
        {
            return OtpErrors.ChannelMismatch;
        }

        var providedHash = hasher.Hash(code);
        var verifyResult = otp.Verify(providedHash, timeProvider.GetUtcNow());

        // Persist the consumed timestamp or the incremented failed-attempt counter.
        await dbContext.SaveChangesAsync(ct);

        if (verifyResult.IsError)
        {
            return verifyResult.Errors;
        }

        return otp.Recipient;
    }

    private async Task PurgeStaleForRecipientAsync(OtpChannel channel, string recipient, DateTimeOffset now, CancellationToken ct)
    {
        try
        {
            await dbContext.OtpCodes
                .Where(o => o.Recipient == recipient
                    && o.Channel == channel
                    && (o.ConsumedAtUtc != null || o.ExpiresAtUtc < now))
                .ExecuteDeleteAsync(ct);
        }
        catch (Exception ex)
        {
            // Cleanup is best-effort; the background job is the safety net.
            logger.LogWarning(ex, "Best-effort OTP purge on issue failed.");
        }
    }

    private async Task<Result<string>> SendEmailAsync(string email, string code, CancellationToken ct)
    {
        var result = await emailSender.SendAsync(
            email,
            "Your Fat7i verification code",
            BuildEmailHtml(code),
            BuildEmailText(code),
            ct);

        return result.IsError ? result.Errors : "email";
    }

    private static string GenerateCode(int length)
    {
        var max = (int)Math.Pow(10, length);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString().PadLeft(length, '0');
    }

    private string BuildSmsBody(string code)
        => $"{code} is your Fat7i verification code. It expires in {_options.ExpiryMinutes} minutes. Do not share it.";

    private string BuildEmailText(string code)
        => $"Your Fat7i verification code is {code}. It expires in {_options.ExpiryMinutes} minutes. "
            + "If you did not request this, you can ignore this email.";

    private string BuildEmailHtml(string code) => $$"""
        <div style="font-family:Arial,Helvetica,sans-serif;max-width:480px;margin:0 auto;padding:24px;color:#2D3142">
          <h2 style="margin:0 0 8px">Fat7i verification</h2>
          <p style="margin:0 0 16px;color:#5b6070">Use this code to continue. It expires in {{_options.ExpiryMinutes}} minutes.</p>
          <div style="font-size:32px;font-weight:700;letter-spacing:8px;color:#d79c5c;padding:12px 0">{{code}}</div>
          <p style="margin:16px 0 0;color:#9aa0ab;font-size:12px">If you did not request this, you can safely ignore this email.</p>
        </div>
        """;
}
