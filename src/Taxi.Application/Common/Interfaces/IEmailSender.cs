using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Abstraction over a transactional email provider (Titan.email via SMTP). Sends
/// simple emails only (e.g. email OTP, verification / welcome mails). Kept free of
/// auth/OTP business logic so the provider can be swapped without touching auth.
/// </summary>
public interface IEmailSender
{
    Task<Result<Success>> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody = null,
        CancellationToken ct = default);
}
