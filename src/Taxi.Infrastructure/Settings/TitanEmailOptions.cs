namespace Taxi.Infrastructure.Settings;

/// <summary>
/// Titan.email SMTP configuration. Bound from <c>Email:Titan</c>. Used only as a
/// simple transactional SMTP sender (e.g. email OTP, verification / welcome mails).
/// Credentials must never be exposed to clients — provide them via user-secrets /
/// environment variables.
/// </summary>
public sealed class TitanEmailOptions
{
    public const string SectionName = "Email:Titan";

    public string Host { get; set; } = "smtp.titan.email";

    /// <summary>465 for implicit SSL, 587 for STARTTLS.</summary>
    public int Port { get; set; } = 465;

    /// <summary>When true use implicit SSL (port 465); when false use STARTTLS (port 587).</summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>SMTP username (usually the full From email address).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SMTP password / app password.</summary>
    public string Password { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "Fat7i";

    /// <summary>Optional Reply-To address.</summary>
    public string? ReplyToEmail { get; set; }
}
