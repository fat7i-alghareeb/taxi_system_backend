using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.Features.Auth.Services;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.Google;

/// <summary>
/// Google sign-in/up. Verifies the Firebase ID token, then matches a returning user by
/// GoogleId or (provider-verified) email → session; otherwise returns a registration
/// challenge (client collects phone → register/complete). The client decides by context:
/// on a login screen a challenge means "No account found"; on a signup screen it proceeds.
/// </summary>
public sealed record GoogleAuthCommand(string FirebaseIdToken, string? FcmToken, string? DeviceId)
    : IRequest<Result<AuthResult>>;

public sealed class GoogleAuthCommandValidator : AbstractValidator<GoogleAuthCommand>
{
    public GoogleAuthCommandValidator()
    {
        RuleFor(x => x.FirebaseIdToken)
            .NotEmpty().WithMessage(LocalizationKeys.Auth.InvalidGoogleToken)
            .MaximumLength(4096).WithMessage(LocalizationKeys.Auth.InvalidGoogleToken);
    }
}

public sealed class GoogleAuthCommandHandler(
    IFirebaseAuthService firebaseAuth,
    IAppDbContext dbContext,
    IRegistrationTokenService registrationTokenService,
    IWelcomeEmailService welcomeEmailService,
    IAuthSessionFactory sessionFactory) : IRequestHandler<GoogleAuthCommand, Result<AuthResult>>
{
    public async Task<Result<AuthResult>> Handle(GoogleAuthCommand request, CancellationToken cancellationToken)
    {
        var identityResult = await firebaseAuth.VerifyIdTokenAndGetIdentityAsync(request.FirebaseIdToken, cancellationToken);
        if (identityResult.IsError)
        {
            return identityResult.Errors;
        }

        var identity = identityResult.Value;

        if (string.IsNullOrWhiteSpace(identity.Email))
        {
            return AuthErrors.GoogleEmailMissing;
        }

        var email = identity.Email.Trim().ToLowerInvariant();

        // Returning user: match by Google uid first, then by an existing verified email
        // (Google emails are provider-verified, so linking to a verified-email account is safe).
        var domainUser = await dbContext.DomainUsers
            .FirstOrDefaultAsync(u => u.GoogleId == identity.Uid && u.IsActive, cancellationToken);

        domainUser ??= await dbContext.DomainUsers
            .FirstOrDefaultAsync(
                u => u.Email != null && u.Email.ToLower() == email && u.IsEmailVerified && u.IsActive,
                cancellationToken);

        if (domainUser is not null)
        {
            if (string.IsNullOrWhiteSpace(domainUser.GoogleId))
            {
                domainUser.LinkGoogle(identity.Uid);
            }

            await FcmTokenSync.ApplyAsync(dbContext, domainUser, request.FcmToken, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Returning Google login. Best-effort — never blocks sign-in. A GoogleId-linked
            // account can have a null Email (e.g. after "start fresh" clears it but keeps the
            // Google link), so guard rather than assume non-null.
            if (!string.IsNullOrWhiteSpace(domainUser.Email))
            {
                await welcomeEmailService.SendWelcomeBackEmailAsync(
                    domainUser.Email, domainUser.Name, domainUser.PreferredLanguage, cancellationToken);
            }

            var sessionResult = await sessionFactory.CreateAsync(domainUser, ct: cancellationToken);
            return sessionResult.IsError ? sessionResult.Errors : AuthResult.ForSession(sessionResult.Value);
        }

        var token = registrationTokenService.Issue(
            new RegistrationTokenPayload(email, identity.Name, identity.Uid, EmailVerified: true));

        return AuthResult.ForRegistration(new RegistrationChallenge(token, email, identity.Name));
    }
}
