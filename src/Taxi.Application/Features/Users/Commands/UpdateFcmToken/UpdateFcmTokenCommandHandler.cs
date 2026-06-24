using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.UpdateFcmToken;

public class UpdateFcmTokenCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<UpdateFcmTokenCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(UpdateFcmTokenCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var userGuid))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Auth.Unauthorized, "Unauthorized user."));
        }

        var duplicateUsers = await _context.DomainUsers
            .Where(user => user.FcmToken == request.FcmToken && user.Id != userGuid)
            .ToListAsync(ct);
        foreach (var duplicateUser in duplicateUsers)
        {
            duplicateUser.UpdateFcmToken(null);
        }

        var duplicateAdmins = await _context.AdminProfiles
            .Where(admin => admin.FcmToken == request.FcmToken && admin.Id != userGuid)
            .ToListAsync(ct);
        foreach (var duplicateAdmin in duplicateAdmins)
        {
            duplicateAdmin.UpdateFcmToken(null);
        }

        if (currentUser.IsAdmin)
        {
            var admin = await _context.AdminProfiles.FirstOrDefaultAsync(a => a.Id == userGuid, ct);
            if (admin is null)
            {
                return Result.Failure<Success>(
                    Error.NotFound(LocalizationKeys.User.NotFound, "Admin profile not found."));
            }

            var adminUpdateResult = admin.UpdateFcmToken(request.FcmToken);
            if (adminUpdateResult.IsFailure)
            {
                return adminUpdateResult.Error;
            }

            await _context.SaveChangesAsync(ct);
            return Result.Success;
        }

        var user = await _context.DomainUsers.FirstOrDefaultAsync(u => u.Id == userGuid, ct);
        if (user is null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.User.NotFound, "User profile not found."));
        }

        var updateResult = user.UpdateFcmToken(request.FcmToken);
        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }
}
