using Microsoft.Extensions.Caching.Memory;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Infrastructure.Auth;

public class OtpService(IMemoryCache cache) : IOtpService
{
    public Task<(string SessionToken, string Code)> GenerateOtpSessionAsync(string phone, CancellationToken cancellationToken)
    {
        var sessionToken = Guid.NewGuid().ToString("N");
        var code = "1234"; // Fixed for testing or use a random generator

        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

        cache.Set($"otp_session_{sessionToken}", (phone, code), options);

        return Task.FromResult((sessionToken, code));
    }

    public Task<Result<string>> VerifyOtpAsync(string sessionToken, string code, CancellationToken cancellationToken)
    {
        if (!cache.TryGetValue($"otp_session_{sessionToken}", out (string Phone, string Code) session))
        {
            return Task.FromResult<Result<string>>(AuthErrors.OtpExpired);
        }

        if (session.Code != code)
        {
            return Task.FromResult<Result<string>>(AuthErrors.InvalidOtp);
        }

        cache.Remove($"otp_session_{sessionToken}");

        return Task.FromResult<Result<string>>(session.Phone);
    }
}
