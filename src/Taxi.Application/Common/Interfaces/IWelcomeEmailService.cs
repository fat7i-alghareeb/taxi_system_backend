namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Sends the localized "thank you for choosing us" welcome email when an account
/// first gains a real email (email/Google sign-up, or a phone user adding an email).
/// Best-effort: implementations must never throw — a failed welcome email must not
/// block account creation or profile updates.
/// </summary>
public interface IWelcomeEmailService
{
    Task SendWelcomeEmailAsync(
        string email,
        string? name,
        string language,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a distinct "welcome back" email when an existing email/Google account logs in
    /// again. Fires on every such login (no throttling) — same best-effort contract.
    /// </summary>
    Task SendWelcomeBackEmailAsync(
        string email,
        string? name,
        string language,
        CancellationToken ct = default);
}
