using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.DeleteCurrentUser;

public class DeleteCurrentUserCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    ISessionRevoker sessionRevoker)
    : IRequestHandler<DeleteCurrentUserCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(DeleteCurrentUserCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var userGuid))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Auth.Unauthorized, "Unauthorized user."));
        }

        var user = await _context.DomainUsers.FirstOrDefaultAsync(u => u.Id == userGuid, ct);
        if (user == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.User.NotFound, "User profile not found."));
        }

        // Clear FCM token so the deleted account stops receiving push notifications.
        user.UpdateFcmToken(null);

        var softDeleteResult = user.SoftDelete();
        if (softDeleteResult.IsFailure)
        {
            return softDeleteResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        // End every device session so the soft-deleted account cannot keep refreshing.
        await sessionRevoker.RevokeAllForUserAsync(userGuid.ToString(), ct);

        return Result.Success;
    }
}
