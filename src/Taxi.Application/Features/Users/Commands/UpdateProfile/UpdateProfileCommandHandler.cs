using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    ILanguageContext languageContext) : IRequestHandler<UpdateProfileCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var user = await context.DomainUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return Error.NotFound(LocalizationKeys.User.NotFound, "User not found.");
        }

        var updateResult = user.UpdateProfile(request.Name);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        return new UserDto
        {
            Id = user.Id,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            Email = user.Email,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            Name = user.Name.GetTranslation(languageContext.Language),
        };
    }
}

