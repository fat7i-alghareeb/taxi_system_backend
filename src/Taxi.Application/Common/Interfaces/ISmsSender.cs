using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Abstraction over an SMS provider (CM.com). Sends plain text messages only.
/// Kept free of auth/OTP business logic so the provider can be swapped without
/// touching the auth flow.
/// </summary>
public interface ISmsSender
{
    /// <summary>
    /// Sends a plain SMS. Returns the provider message id/reference on success.
    /// </summary>
    /// <param name="toE164Phone">Recipient in international format, e.g. +31612345678.</param>
    /// <param name="message">Plain text message body.</param>
    Task<Result<string>> SendAsync(string toE164Phone, string message, CancellationToken ct = default);
}
