using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;
using Taxi.Infrastructure.Settings;

namespace Taxi.Infrastructure.Sms;

/// <summary>
/// Sends plain SMS through the CM.com Business Messaging API
/// (<c>POST {BaseUrl}v1.0/message</c>). Uses a typed <see cref="HttpClient"/>; the
/// product token stays server-side. Not CM.com OTP-as-a-service — the backend owns all
/// OTP logic and only uses CM.com as a dumb SMS pipe. <c>allowedChannels: ["SMS"]</c>
/// forces SMS (never WhatsApp/OTT).
/// </summary>
internal sealed class CmComSmsSender(
    HttpClient httpClient,
    IOptions<CmComSmsOptions> options,
    ILogger<CmComSmsSender> logger) : ISmsSender
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CmComSmsOptions _options = options.Value;

    public async Task<Result<string>> SendAsync(string toE164Phone, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ProductToken))
        {
            logger.LogError("CM.com product token is not configured; cannot send SMS.");
            return OtpErrors.SendFailed;
        }

        var reference = Guid.NewGuid().ToString("N");

        var payload = new CmRequest(
            new CmMessages(
                new CmAuthentication(_options.ProductToken),
                [
                    new CmMessage(
                        _options.Sender,
                        [new CmRecipient(toE164Phone)],
                        new CmBody(message),
                        reference,
                        ["SMS"])
                ]));

        try
        {
            using var response = await httpClient.PostAsJsonAsync("v1.0/message", payload, JsonOptions, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                // Never log the message body/recipient in full; mask the recipient.
                logger.LogError(
                    "CM.com SMS send failed for {MaskedPhone}. Status={Status} Body={Body}",
                    Mask(toE164Phone),
                    (int)response.StatusCode,
                    raw);
                return OtpErrors.SendFailed;
            }

            var providerId = TryExtractReference(raw) ?? reference;
            logger.LogInformation(
                "CM.com SMS accepted for {MaskedPhone}. Reference={Reference}",
                Mask(toE164Phone),
                providerId);

            return providerId;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "CM.com SMS send threw for {MaskedPhone}.", Mask(toE164Phone));
            return OtpErrors.SendFailed;
        }
    }

    private static string? TryExtractReference(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (doc.RootElement.TryGetProperty("messages", out var messages)
                && messages.ValueKind == JsonValueKind.Array
                && messages.GetArrayLength() > 0)
            {
                var first = messages[0];
                if (first.TryGetProperty("reference", out var referenceProp))
                {
                    return referenceProp.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // Response shape can vary; the caller falls back to the client reference.
        }

        return null;
    }

    private static string Mask(string phone)
        => phone.Length <= 4 ? "***" : string.Concat(phone.AsSpan(0, 3), "****", phone.AsSpan(phone.Length - 2));

    // CM.com Business Messaging API request shape.
    private sealed record CmRequest([property: JsonPropertyName("messages")] CmMessages Messages);

    private sealed record CmMessages(
        [property: JsonPropertyName("authentication")] CmAuthentication Authentication,
        [property: JsonPropertyName("msg")] IReadOnlyList<CmMessage> Msg);

    private sealed record CmAuthentication(
        [property: JsonPropertyName("productToken")] string ProductToken);

    private sealed record CmMessage(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] IReadOnlyList<CmRecipient> To,
        [property: JsonPropertyName("body")] CmBody Body,
        [property: JsonPropertyName("reference")] string Reference,
        [property: JsonPropertyName("allowedChannels")] IReadOnlyList<string> AllowedChannels);

    private sealed record CmRecipient([property: JsonPropertyName("number")] string Number);

    private sealed record CmBody(
        [property: JsonPropertyName("content")] string Content,
        [property: JsonPropertyName("type")] string Type = "AUTO");
}
