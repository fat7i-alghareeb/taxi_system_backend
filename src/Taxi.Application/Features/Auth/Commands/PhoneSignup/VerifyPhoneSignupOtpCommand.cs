using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.Features.Auth.Services;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.PhoneSignup;

/// <summary>
/// Phone SIGNUP — step 2. Verifies the code, then creates a fresh account (verified phone,
/// placeholder name → client shows the name step) or, if an active account already exists,
/// returns its session flagged <c>AccountAlreadyExists</c> so the client can offer
/// Continue / Start fresh.
/// </summary>
public sealed record VerifyPhoneSignupOtpCommand(Guid OtpRequestId, string Code, string? FcmToken)
    : IRequest<Result<AuthResponse>>;

public sealed class VerifyPhoneSignupOtpCommandValidator : AbstractValidator<VerifyPhoneSignupOtpCommand>
{
    public VerifyPhoneSignupOtpCommandValidator()
    {
        RuleFor(x => x.OtpRequestId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty();
    }
}

public sealed class VerifyPhoneSignupOtpCommandHandler(
    IAppDbContext dbContext,
    IOtpService otpService,
    IIdentityService identityService,
    IAuthSessionFactory sessionFactory) : IRequestHandler<VerifyPhoneSignupOtpCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(VerifyPhoneSignupOtpCommand request, CancellationToken cancellationToken)
    {
        var verifyResult = await otpService.VerifyAsync(
            request.OtpRequestId,
            request.Code,
            OtpChannel.Sms,
            OtpPurpose.PhoneSignup,
            cancellationToken);

        if (verifyResult.IsError)
        {
            return verifyResult.Errors;
        }

        var phone = verifyResult.Value;

        var identityResult = await identityService.GetOrCreateUserByPhoneAsync(phone, UserRole.Passenger.ToString());
        if (identityResult.IsError)
        {
            return identityResult.Errors;
        }

        var identityId = identityResult.Value;
        var placeholder = $"Passenger {phone}";

        // IgnoreQueryFilters so a soft-deleted account on this phone is found and revived,
        // rather than colliding on a duplicate insert.
        var domainUser = await dbContext.DomainUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Phone == phone, cancellationToken);

        var isNewAccount = false;
        var accountAlreadyExists = false;

        if (domainUser is null)
        {
            var createResult = User.Create(
                Guid.Parse(identityId),
                placeholder,
                phone,
                null,
                UserRole.Passenger,
                isPhoneVerified: true);

            if (createResult.IsError)
            {
                return createResult.Errors;
            }

            domainUser = createResult.Value;
            dbContext.DomainUsers.Add(domainUser);
            isNewAccount = true;
        }
        else if (domainUser.DeletedAtUtc is not null)
        {
            var reviveResult = domainUser.ReviveForReRegistration(placeholder);
            if (reviveResult.IsError)
            {
                return reviveResult.Errors;
            }

            isNewAccount = true;
        }
        else if (!domainUser.IsActive)
        {
            return UserErrors.Inactive;
        }
        else
        {
            // Active account already exists for this proven phone → let the client offer
            // Continue / Start fresh.
            domainUser.MarkPhoneVerified();
            accountAlreadyExists = true;
        }

        await FcmTokenSync.ApplyAsync(dbContext, domainUser, request.FcmToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await sessionFactory.CreateAsync(
            domainUser,
            isNewAccount: isNewAccount,
            accountAlreadyExists: accountAlreadyExists,
            ct: cancellationToken);
    }
}
