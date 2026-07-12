namespace Taxi.Application.Common.Interfaces;

using System.Security.Claims;
using Taxi.Application.Features.Identity.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Identity;

public interface ITokenProvider
{
    Task<Result<TokenResponse>> GenerateJwtTokenAsync(AppUserDto user, CancellationToken ct = default);

    /// <summary>
    /// Rotates a specific refresh-token row: atomically revokes only that row and
    /// issues a new token pair, without touching any other session's refresh token
    /// for the same user.
    /// </summary>
    Task<Result<TokenResponse>> RotateAsync(RefreshToken presentedToken, AppUserDto user, CancellationToken ct = default);

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

