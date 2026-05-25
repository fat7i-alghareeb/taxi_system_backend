using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.UpdatePreferredLanguage;

public class UpdatePreferredLanguageCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<UpdatePreferredLanguageCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(UpdatePreferredLanguageCommand request, CancellationToken ct)
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

        var updateResult = user.UpdatePreferredLanguage(request.LanguageCode);
        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }
}
