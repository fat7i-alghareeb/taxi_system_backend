using Taxi.Application.Features.Auth.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Common.Interfaces;

/// <summary>
/// Builds the signed session (<see cref="AuthResponse"/>) for a resolved domain user:
/// issues the JWT pair and assembles the client <see cref="UserDto"/> (placeholder-name
/// hiding, driver info, verification flags, home address).
/// </summary>
public interface IAuthSessionFactory
{
    Task<Result<AuthResponse>> CreateAsync(
        User domainUser,
        bool isNewAccount = false,
        bool accountAlreadyExists = false,
        CancellationToken ct = default);
}
