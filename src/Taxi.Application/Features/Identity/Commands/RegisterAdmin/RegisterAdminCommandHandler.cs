using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Admins;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Identity.Commands.RegisterAdmin;

public class RegisterAdminCommandHandler(
    IIdentityService identityService,
    IAppDbContext dbContext) : IRequestHandler<RegisterAdminCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterAdminCommand request, CancellationToken cancellationToken)
    {
        var identityResult = await identityService.CreateAdminUserAsync(
            request.UserName,
            request.Email,
            request.Password);

        if (identityResult.IsFailure)
        {
            return identityResult.Error;
        }

        var identityId = identityResult.Value;

        var adminProfileResult = AdminProfile.Create(
            Guid.Parse(identityId),
            request.Name,
            request.Email,
            request.Phone1,
            request.Phone2);

        if (adminProfileResult.IsFailure)
        {
            return adminProfileResult.Errors;
        }

        dbContext.AdminProfiles.Add(adminProfileResult.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        return adminProfileResult.Value.Id;
    }
}

