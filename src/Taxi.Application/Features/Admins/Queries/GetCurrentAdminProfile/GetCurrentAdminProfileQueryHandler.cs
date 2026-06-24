using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Admins.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Admins.Queries.GetCurrentAdminProfile;

public class GetCurrentAdminProfileQueryHandler(
    IAppDbContext context,
    IUser currentUser) : IRequestHandler<GetCurrentAdminProfileQuery, Result<AdminProfileDto>>
{
    public async Task<Result<AdminProfileDto>> Handle(GetCurrentAdminProfileQuery request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var adminId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var admin = await context.AdminProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == adminId, ct);
        if (admin is null)
        {
            return Error.NotFound(LocalizationKeys.User.NotFound, "Admin profile not found.");
        }

        return new AdminProfileDto(
            admin.Id,
            admin.Name,
            admin.Email,
            admin.Phone1,
            admin.Phone2,
            admin.IsActive);
    }
}
