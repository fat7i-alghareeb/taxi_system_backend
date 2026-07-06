using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.Features.Auth.Services;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.CompleteRegistration;

/// <summary>
/// Finalizes a Google/email sign-up: validates the registration token, enforces
/// verified-email uniqueness, and creates the account with the collected phone stored as
/// UNVERIFIED (the app then offers Verify now / Skip for now). The email is stored verified.
/// </summary>
public sealed record CompleteRegistrationCommand(string RegistrationToken, string Name, string Phone, string? FcmToken)
    : IRequest<Result<AuthResponse>>;

public sealed class CompleteRegistrationCommandValidator : AbstractValidator<CompleteRegistrationCommand>
{
    public CompleteRegistrationCommandValidator()
    {
        RuleFor(x => x.RegistrationToken)
            .NotEmpty().WithMessage(LocalizationKeys.Auth.RegistrationTokenInvalid);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(LocalizationKeys.User.NameRequired);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage(LocalizationKeys.User.PhoneRequired)
            .Matches(@"^\+?\d{7,15}$").WithMessage(LocalizationKeys.Validation.PhoneNumberFormat);
    }
}

public sealed class CompleteRegistrationCommandHandler(
    IRegistrationTokenService registrationTokenService,
    IAppDbContext dbContext,
    IIdentityService identityService,
    IAuthSessionFactory sessionFactory) : IRequestHandler<CompleteRegistrationCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(CompleteRegistrationCommand request, CancellationToken cancellationToken)
    {
        var payloadResult = registrationTokenService.Validate(request.RegistrationToken);
        if (payloadResult.IsError)
        {
            return payloadResult.Errors;
        }

        var payload = payloadResult.Value;
        var email = payload.Email.Trim().ToLowerInvariant();
        var phone = PhoneNumberNormalizer.Normalize(request.Phone);

        // Verified-email uniqueness (also enforced by the partial DB index).
        var emailTaken = await dbContext.DomainUsers
            .AnyAsync(u => u.Email != null && u.Email.ToLower() == email && u.IsEmailVerified && u.IsActive, cancellationToken);

        if (emailTaken)
        {
            return AuthErrors.EmailAlreadyRegistered;
        }

        if (!string.IsNullOrWhiteSpace(payload.GoogleId))
        {
            var googleTaken = await dbContext.DomainUsers
                .AnyAsync(u => u.GoogleId == payload.GoogleId && u.IsActive, cancellationToken);

            if (googleTaken)
            {
                return AuthErrors.AccountAlreadyExists;
            }
        }

        var identityResult = await identityService.CreatePasswordlessUserAsync(email, phone, UserRole.Passenger.ToString());
        if (identityResult.IsError)
        {
            return identityResult.Errors;
        }

        // Phone is stored UNVERIFIED (no uniqueness block); email is stored VERIFIED.
        var createResult = User.Create(
            Guid.Parse(identityResult.Value),
            request.Name.Trim(),
            phone,
            email,
            UserRole.Passenger,
            isPhoneVerified: false,
            isEmailVerified: payload.EmailVerified,
            googleId: payload.GoogleId);

        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var domainUser = createResult.Value;
        dbContext.DomainUsers.Add(domainUser);

        await FcmTokenSync.ApplyAsync(dbContext, domainUser, request.FcmToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await sessionFactory.CreateAsync(domainUser, isNewAccount: true, ct: cancellationToken);
    }
}
