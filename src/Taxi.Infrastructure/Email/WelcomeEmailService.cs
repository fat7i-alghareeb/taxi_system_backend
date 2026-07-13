using System.Globalization;

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Infrastructure.Settings;

namespace Taxi.Infrastructure.Email;

/// <summary>
/// Builds and sends the localized welcome email through <see cref="IEmailSender"/>.
/// Localization mirrors the FcmNotificationService pattern (temporarily swap
/// <see cref="CultureInfo.CurrentUICulture"/> around a shared-resource lookup).
/// Fully best-effort: any failure is logged and swallowed.
/// </summary>
internal sealed class WelcomeEmailService(
    IEmailSender emailSender,
    IStringLocalizerFactory localizerFactory,
    IOptions<TitanEmailOptions> emailOptions,
    ILogger<WelcomeEmailService> logger) : IWelcomeEmailService
{
    private readonly TitanEmailOptions _emailOptions = emailOptions.Value;

    public Task SendWelcomeEmailAsync(string email, string? name, string language, CancellationToken ct = default)
        => SendAsync(
            "Welcome email",
            email,
            name,
            language,
            LocalizationKeys.Email.WelcomeSubject,
            LocalizationKeys.Email.WelcomeHeading,
            LocalizationKeys.Email.WelcomeBody,
            LocalizationKeys.Email.WelcomeSignoff,
            ct);

    public Task SendWelcomeBackEmailAsync(string email, string? name, string language, CancellationToken ct = default)
        => SendAsync(
            "Welcome-back email",
            email,
            name,
            language,
            LocalizationKeys.Email.WelcomeBackSubject,
            LocalizationKeys.Email.WelcomeBackHeading,
            LocalizationKeys.Email.WelcomeBackBody,
            LocalizationKeys.Email.WelcomeBackSignoff,
            ct);

    private async Task SendAsync(
        string logLabel,
        string email,
        string? name,
        string language,
        string subjectKey,
        string headingKey,
        string bodyKey,
        string signoffKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        try
        {
            var lang = string.IsNullOrWhiteSpace(language) ? Languages.Default : language;
            var (subject, heading, body, signoff) = Localize(lang, subjectKey, headingKey, bodyKey, signoffKey);

            var html = BuildHtml(heading, name, body, signoff);
            var text = BuildText(heading, name, body, signoff);

            var result = await emailSender.SendAsync(email, subject, html, text, ct);
            if (result.IsError)
            {
                logger.LogWarning(
                    "{Label} not delivered ({Error}).",
                    logLabel,
                    result.Errors.FirstOrDefault().Code);
            }
        }
        catch (Exception ex)
        {
            // Never let a welcome email break account creation / login / profile update.
            logger.LogWarning(ex, "Failed to send {Label}.", logLabel);
        }
    }

    private (string Subject, string Heading, string Body, string Signoff) Localize(
        string lang, string subjectKey, string headingKey, string bodyKey, string signoffKey)
    {
        var localizer = localizerFactory.Create("Taxi.Api.SharedResource", "Taxi.Api");
        var originalCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(lang);
            return (
                localizer[subjectKey],
                localizer[headingKey],
                localizer[bodyKey],
                localizer[signoffKey]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    private string BuildText(string heading, string? name, string body, string signoff)
    {
        var greeting = string.IsNullOrWhiteSpace(name) ? string.Empty : $"{name.Trim()}\n\n";
        return $"{heading}\n\n{greeting}{body}\n\n{signoff}\n{_emailOptions.FromName}";
    }

    private string BuildHtml(string heading, string? name, string body, string signoff)
    {
        var greeting = string.IsNullOrWhiteSpace(name)
            ? string.Empty
            : $"<p style=\"margin:0 0 12px;font-weight:600;color:#2D3142\">{System.Net.WebUtility.HtmlEncode(name.Trim())}</p>";

        return $$"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:480px;margin:0 auto;padding:24px;color:#2D3142">
              <h2 style="margin:0 0 16px;color:#d79c5c">{{System.Net.WebUtility.HtmlEncode(heading)}}</h2>
              {{greeting}}
              <p style="margin:0 0 16px;color:#5b6070;line-height:1.5">{{System.Net.WebUtility.HtmlEncode(body)}}</p>
              <p style="margin:24px 0 0;color:#9aa0ab;font-size:13px">{{System.Net.WebUtility.HtmlEncode(signoff)}}</p>
              <p style="margin:2px 0 0;color:#9aa0ab;font-size:13px;font-weight:600">{{System.Net.WebUtility.HtmlEncode(_emailOptions.FromName)}}</p>
            </div>
            """;
    }
}
