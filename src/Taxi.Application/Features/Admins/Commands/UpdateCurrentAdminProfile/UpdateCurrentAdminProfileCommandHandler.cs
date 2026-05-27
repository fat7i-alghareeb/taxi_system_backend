using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Admins.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Admins.Commands.UpdateCurrentAdminProfile;

public class UpdateCurrentAdminProfileCommandHandler(
    IAppDbContext context,
    IUser currentUser) : IRequestHandler<UpdateCurrentAdminProfileCommand, Result<AdminProfileDto>>
{
    public async Task<Result<AdminProfileDto>> Handle(UpdateCurrentAdminProfileCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var adminId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var admin = await context.AdminProfiles.FirstOrDefaultAsync(a => a.Id == adminId, ct);
        if (admin is null)
        {
            return Error.NotFound(LocalizationKeys.User.NotFound, "Admin profile not found.");
        }

        var updateResult = admin.Update(request.Name, request.Email, request.Phone1, request.Phone2);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        return new AdminProfileDto(
            admin.Id,
            admin.Name,
            admin.Email,
            admin.Phone1,
            admin.Phone2,
            admin.IsActive);
    }
}
