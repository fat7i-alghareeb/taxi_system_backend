using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.ContinueExistingAccount;

/// <summary>
/// Acknowledges that the user chose "Continue" on the existing-account dialog (the
/// alternative to "Start fresh"). Pure notification ack — no data changes, just sends
/// the welcome-back email so exactly one of the two collision emails goes out.
/// </summary>
public sealed record ContinueExistingAccountCommand : IRequest<Result<Success>>;

public sealed class ContinueExistingAccountCommandValidator : AbstractValidator<ContinueExistingAccountCommand>
{
    // No client-supplied fields; the acting user comes from the JWT.
}

public sealed class ContinueExistingAccountCommandHandler(
    IAppDbContext dbContext,
    IUser currentUser,
    IWelcomeEmailService welcomeEmailService) : IRequestHandler<ContinueExistingAccountCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ContinueExistingAccountCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return UserErrors.NotFound;
        }

        var domainUser = await dbContext.DomainUsers
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);

        if (domainUser is null)
        {
            return UserErrors.NotFound;
        }

        // Only email/Google-collision accounts have an email to welcome back to;
        // a phone-collision account with no email is a silent no-op here.
        if (!string.IsNullOrWhiteSpace(domainUser.Email))
        {
            await welcomeEmailService.SendWelcomeBackEmailAsync(
                domainUser.Email, domainUser.Name, domainUser.PreferredLanguage, ct);
        }

        return Result.Success;
    }
}
