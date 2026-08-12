using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(ISessionRevoker sessionRevoker, IUser currentUser)
    : IRequestHandler<LogoutCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(LogoutCommand request, CancellationToken ct)
    {
        var userId = currentUser.Id;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "User is not authenticated.");
        }

        if (request.AllDevices)
        {
            await sessionRevoker.RevokeAllForUserAsync(userId, ct);
            return Result.Success;
        }

        // Revocation is scoped to the caller's own tokens, so a token belonging to another
        // account can never be revoked by presenting it here. Succeed regardless of whether
        // a live row was found — logout must be idempotent and must not reveal whether a
        // given token string exists.
        await sessionRevoker.RevokeTokenAsync(userId, request.RefreshToken!, ct);
        return Result.Success;
    }
}
