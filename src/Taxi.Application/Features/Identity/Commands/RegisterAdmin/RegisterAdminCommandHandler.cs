using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Identity.Commands.RegisterAdmin;

public class RegisterAdminCommandHandler(
    IIdentityService identityService,
    IAppDbContext dbContext) : IRequestHandler<RegisterAdminCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterAdminCommand request, CancellationToken cancellationToken)
    {
        // 1. Create Identity user via service
        var identityResult = await identityService.CreateUserAsync(
            request.Phone,
            request.Email ?? string.Empty,
            request.Password,
            "Admin");

        if (identityResult.IsFailure)
        {
            return identityResult.Error;
        }

        var identityId = identityResult.Value;

        // 2. Create domain User
        var domainUserResult = User.Create(
            Guid.Parse(identityId),
            request.NameEn,
            request.NameAr,
            request.NameNl,
            request.NameDe,
            request.NamePl,
            request.NameUk,
            request.NameFr,
            request.NameEs,
            request.NameRo,
            request.Phone,
            request.Email,
            UserRole.Admin);

        if (domainUserResult.IsFailure)
        {
            return domainUserResult.Errors;
        }

        dbContext.DomainUsers.Add(domainUserResult.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        return domainUserResult.Value.Id;
    }
}

