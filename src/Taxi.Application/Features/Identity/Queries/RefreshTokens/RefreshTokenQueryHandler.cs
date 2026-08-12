namespace Taxi.Application.Features.Identity.Queries.RefreshTokens;

using System.Security.Claims;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taxi.Application.Common.Errors;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Identity.Dtos;
using Taxi.Domain.Common.Results;

public class RefreshTokenQueryHandler(
    ILogger<RefreshTokenQueryHandler> logger,
    IIdentityService identityService,
    IAppDbContext context,
    ITokenProvider tokenProvider)
    : IRequestHandler<RefreshTokenQuery, Result<TokenResponse>>
{
    private readonly ILogger<RefreshTokenQueryHandler> logger = logger;
    private readonly IIdentityService identityService = identityService;
    private readonly IAppDbContext context = context;
    private readonly ITokenProvider tokenProvider = tokenProvider;

    public async Task<Result<TokenResponse>> Handle(RefreshTokenQuery request, CancellationToken ct)
    {
        var principal = this.tokenProvider.GetPrincipalFromExpiredToken(request.ExpiredAccessToken);

        if (principal is null)
        {
            this.logger.LogError("Expired access token is not valid");
            return ApplicationErrors.ExpiredAccessTokenInvalid;
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId is null)
        {
            this.logger.LogError("Invalid userId claim");
            return ApplicationErrors.UserIdClaimInvalid;
        }

        var getUserResult = await this.identityService.GetUserByIdAsync(userId);

        if (getUserResult.IsError)
        {
            this.logger.LogError("Get user by id error occurred: {ErrorDescription}", getUserResult.TopError.Description);
            return getUserResult.Errors;
        }

        // Re-check account state on every refresh. IsActive is otherwise only consulted at
        // login, so a suspended or self-deleted account could keep minting access tokens
        // from a refresh token issued before the suspension.
        if (!Guid.TryParse(userId, out var domainUserId))
        {
            this.logger.LogError("User id claim is not a GUID for user {UserId}", userId);
            return ApplicationErrors.UserIdClaimInvalid;
        }

        // Admins have no DomainUsers row (they are AppUser + AdminProfile), so both stores
        // are consulted — checking only DomainUsers would lock every admin out of refresh.
        var isActivePassengerOrDriver = await this.context.DomainUsers
            .AsNoTracking()
            .AnyAsync(u => u.Id == domainUserId && u.IsActive, ct);

        var isActiveAdmin = !isActivePassengerOrDriver
            && await this.context.AdminProfiles
                .AsNoTracking()
                .AnyAsync(a => a.Id == domainUserId && a.IsActive, ct);

        if (!isActivePassengerOrDriver && !isActiveAdmin)
        {
            this.logger.LogWarning(
                "[Security] Refresh denied — account {UserId} is suspended, deleted, or missing.",
                userId);
            return ApplicationErrors.RefreshTokenExpired;
        }

        var refreshToken = await this.context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken && r.UserId == userId, ct);

        if (refreshToken is null)
        {
            this.logger.LogError("Refresh token was not found for user {UserId}", userId);
            return ApplicationErrors.RefreshTokenExpired;
        }

        if (refreshToken.ExpiresOnUtc < DateTimeOffset.UtcNow)
        {
            this.logger.LogError(
                "Refresh token has expired for user {UserId}. ExpiresOnUtc={ExpiresOnUtc}",
                userId,
                refreshToken.ExpiresOnUtc);

            return ApplicationErrors.RefreshTokenExpired;
        }

        var generateTokenResult = await this.tokenProvider.RotateAsync(refreshToken, getUserResult.Value, ct);

        if (generateTokenResult.IsError)
        {
            this.logger.LogError("Generate token error occurred: {ErrorDescription}", generateTokenResult.TopError.Description);
            return generateTokenResult.Errors;
        }

        return generateTokenResult.Value;
    }
}

