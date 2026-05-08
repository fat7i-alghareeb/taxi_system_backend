using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

public interface IOtpService
{
    Task<(string SessionToken, string Code)> GenerateOtpSessionAsync(string phone, CancellationToken cancellationToken = default);

    Task<Result<string>> VerifyOtpAsync(string sessionToken, string otp, CancellationToken cancellationToken = default);
}

