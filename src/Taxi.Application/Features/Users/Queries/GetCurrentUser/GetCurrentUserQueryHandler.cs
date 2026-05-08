using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler(
    IAppDbContext context,
    IUser currentUser,
    ILanguageContext languageContext) : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var user = await context.DomainUsers
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return Error.NotFound(LocalizationKeys.User.NotFound, "User not found.");
        }

        var resolvedName = user.Name.GetTranslation(languageContext.Language);
        var isPlaceholder = resolvedName.StartsWith("Passenger ") || resolvedName.StartsWith("راكب ") || resolvedName.StartsWith("Passagier ");

        return new UserDto
        {
            Id = user.Id,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            Email = user.Email,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            Name = isPlaceholder ? null : resolvedName,
        };
    }
}

