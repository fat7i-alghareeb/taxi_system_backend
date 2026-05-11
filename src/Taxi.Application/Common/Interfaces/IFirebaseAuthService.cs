using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

public interface IFirebaseAuthService
{
    Task<Result<string>> VerifyIdTokenAndGetPhoneAsync(string idToken, CancellationToken ct = default);
}
