using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MimeKit;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;
using Taxi.Infrastructure.Settings;

namespace Taxi.Infrastructure.Email;

/// <summary>
/// Sends simple transactional emails through Titan.email SMTP using MailKit. Credentials
/// stay server-side. Contains no auth/OTP business logic — just delivery.
/// </summary>
internal sealed class TitanEmailSender(
    IOptions<TitanEmailOptions> options,
    ILogger<TitanEmailSender> logger) : IEmailSender
{
    private readonly TitanEmailOptions _options = options.Value;

    public async Task<Result<Success>> SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host)
            || string.IsNullOrWhiteSpace(_options.Username)
            || string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            logger.LogError("Titan SMTP is not configured; cannot send email.");
            return OtpErrors.SendFailed;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            if (!string.IsNullOrWhiteSpace(_options.ReplyToEmail))
            {
                message.ReplyTo.Add(MailboxAddress.Parse(_options.ReplyToEmail));
            }

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = string.IsNullOrWhiteSpace(textBody) ? StripHtml(htmlBody) : textBody,
            };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            var socketOptions = _options.UseSsl
                ? SecureSocketOptions.SslOnConnect      // implicit TLS (port 465)
                : SecureSocketOptions.StartTls;         // STARTTLS (port 587)

            await client.ConnectAsync(_options.Host, _options.Port, socketOptions, ct);
            await client.AuthenticateAsync(_options.Username, _options.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Titan email sent to {MaskedEmail} (subject: {Subject}).", Mask(toEmail), subject);
            return Result.Success;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Titan email send failed to {MaskedEmail}.", Mask(toEmail));
            return OtpErrors.SendFailed;
        }
    }

    private static string StripHtml(string html)
        => System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);

    private static string Mask(string email)
    {
        var at = email.IndexOf('@', StringComparison.Ordinal);
        if (at <= 1)
        {
            return "***";
        }

        return string.Concat(email.AsSpan(0, 1), "***", email.AsSpan(at));
    }
}
