using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Admins.Commands.ChangeAdminPassword;

public class ChangeAdminPasswordCommandHandler(
    IIdentityService identityService,
    ISessionRevoker sessionRevoker,
    IUser currentUser) : IRequestHandler<ChangeAdminPasswordCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ChangeAdminPasswordCommand request, CancellationToken ct)
    {
        var userId = currentUser.Id;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "User is not authenticated.");
        }

        var result = await identityService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        if (result.IsError)
        {
            return result;
        }

        // Admin credentials are the highest-value target on the platform — a password
        // change must terminate any session an attacker already holds.
        await sessionRevoker.RevokeAllForUserAsync(userId, ct);

        return result;
    }
}
