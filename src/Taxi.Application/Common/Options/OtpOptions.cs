namespace Taxi.Application.Common.Options;

/// <summary>
/// Backend-owned OTP policy. Bound from the <c>Otp</c> section in appsettings.
/// The backend owns generation, hashing, expiry, resend cooldown, attempt limits
/// and retention — no external OTP-as-a-service is used.
/// </summary>
public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    /// <summary>Number of digits in a generated code.</summary>
    public int CodeLength { get; set; } = 6;

    /// <summary>How long a code stays valid after it is issued.</summary>
    public int ExpiryMinutes { get; set; } = 5;

    /// <summary>Maximum failed verification attempts before a code is locked.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Minimum seconds between two sends to the same recipient/purpose.</summary>
    public int ResendCooldownSeconds { get; set; } = 60;

    /// <summary>Expired/consumed rows older than this are purged by the cleanup job.</summary>
    public int OtpRetentionDays { get; set; } = 30;

    /// <summary>
    /// Server-side secret used to HMAC codes at rest. Never commit a real value —
    /// provide it via user-secrets / environment variables.
    /// </summary>
    public string HashSecret { get; set; } = string.Empty;

    /// <summary>
    /// Store-reviewer bypass: when a code is requested for exactly this recipient
    /// (normalized phone/email, case-insensitive), a fixed reusable code is issued
    /// instead of a random one and no real SMS/email is sent. Empty disables the
    /// bypass entirely. Never commit a real value — provide it via user-secrets /
    /// environment variables, and use a recipient that isn't a real customer's.
    /// </summary>
    public string ReviewBypassRecipient { get; set; } = string.Empty;

    /// <summary>The fixed code returned for <see cref="ReviewBypassRecipient"/>. Must be
    /// set (via user-secrets/env) whenever <see cref="ReviewBypassRecipient"/> is set.</summary>
    public string ReviewBypassCode { get; set; } = string.Empty;
}
