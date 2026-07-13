using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.FreshStart;

/// <summary>
/// "Start fresh" for the authenticated user: wipes personal profile data and stamps
/// ProfileResetAtUtc so the user stops seeing pre-reset history. No hard delete — the
/// account id, phone verification and all historical rows stay for admin/accounting.
/// Returns a refreshed session (name is now a placeholder → client shows the name step).
/// </summary>
public sealed record FreshStartCommand : IRequest<Result<AuthResponse>>;

public sealed class FreshStartCommandValidator : AbstractValidator<FreshStartCommand>
{
    // No client-supplied fields; the acting user comes from the JWT.
}

public sealed class FreshStartCommandHandler(
    IAppDbContext dbContext,
    IUser currentUser,
    IAuthSessionFactory sessionFactory,
    IWelcomeEmailService welcomeEmailService,
    TimeProvider timeProvider) : IRequestHandler<FreshStartCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(FreshStartCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return UserErrors.NotFound;
        }

        var domainUser = await dbContext.DomainUsers
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        if (domainUser is null)
        {
            return UserErrors.NotFound;
        }

        var previousEmail = domainUser.Email; // capture before ResetForFreshStart wipes it

        var resetResult = domainUser.ResetForFreshStart(
            $"Passenger {domainUser.Phone}",
            timeProvider.GetUtcNow());

        if (resetResult.IsError)
        {
            return resetResult.Errors;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Only an email/Google-collision account had an email to notify; a phone-only
        // collision has nothing to send to. Best-effort — never blocks the fresh start.
        if (!string.IsNullOrWhiteSpace(previousEmail))
        {
            await welcomeEmailService.SendWelcomeEmailAsync(
                previousEmail, name: null, domainUser.PreferredLanguage, cancellationToken);
        }

        return await sessionFactory.CreateAsync(domainUser, isNewAccount: true, ct: cancellationToken);
    }
}
