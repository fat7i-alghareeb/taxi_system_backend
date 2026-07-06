using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Auth.Commands.EmailSignup;

/// <summary>
/// Email SIGNUP — step 2. Verifies email ownership. If a verified-email account already
/// exists, returns its session (flagged <c>AccountAlreadyExists</c>). Otherwise returns a
/// registration challenge — the client collects name + phone, then calls register/complete.
/// </summary>
public sealed record VerifyEmailSignupOtpCommand(Guid OtpRequestId, string Code)
    : IRequest<Result<AuthResult>>;

public sealed class VerifyEmailSignupOtpCommandValidator : AbstractValidator<VerifyEmailSignupOtpCommand>
{
    public VerifyEmailSignupOtpCommandValidator()
    {
        RuleFor(x => x.OtpRequestId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty();
    }
}

public sealed class VerifyEmailSignupOtpCommandHandler(
    IAppDbContext dbContext,
    IOtpService otpService,
    IRegistrationTokenService registrationTokenService,
    IAuthSessionFactory sessionFactory) : IRequestHandler<VerifyEmailSignupOtpCommand, Result<AuthResult>>
{
    public async Task<Result<AuthResult>> Handle(VerifyEmailSignupOtpCommand request, CancellationToken cancellationToken)
    {
        var verifyResult = await otpService.VerifyAsync(
            request.OtpRequestId,
            request.Code,
            OtpChannel.Email,
            OtpPurpose.EmailSignup,
            cancellationToken);

        if (verifyResult.IsError)
        {
            return verifyResult.Errors;
        }

        var email = verifyResult.Value;

        var existing = await dbContext.DomainUsers
            .FirstOrDefaultAsync(
                u => u.Email != null && u.Email.ToLower() == email && u.IsEmailVerified && u.IsActive,
                cancellationToken);

        if (existing is not null)
        {
            var sessionResult = await sessionFactory.CreateAsync(existing, accountAlreadyExists: true, ct: cancellationToken);
            return sessionResult.IsError ? sessionResult.Errors : AuthResult.ForSession(sessionResult.Value);
        }

        var token = registrationTokenService.Issue(
            new RegistrationTokenPayload(email, Name: null, GoogleId: null, EmailVerified: true));

        return AuthResult.ForRegistration(new RegistrationChallenge(token, email, Name: null));
    }
}
