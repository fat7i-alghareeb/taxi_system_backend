using Taxi.Application.Common.Interfaces;

namespace Taxi.Application.Features.Auth.Dtos;

/// <summary>
/// Returned by every request-OTP endpoint. The client verifies with
/// <see cref="OtpRequestId"/> + code (not by re-sending the raw recipient).
/// </summary>
public sealed record OtpRequestResponse(Guid OtpRequestId, int ExpiresInSeconds, int ResendAvailableInSeconds)
{
    public static OtpRequestResponse From(OtpIssueResult result)
        => new(result.OtpRequestId, result.ExpiresInSeconds, result.ResendAvailableInSeconds);
}
