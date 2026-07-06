using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.PhoneVerify;

/// <summary>
/// Authorized "Verify now" — step 2. Verifies the code, sets the current account's phone
/// (if changed) and marks it verified, then returns a refreshed session so the client can
/// clear its unverified-phone warnings.
/// </summary>
public sealed record VerifyPhoneVerifyOtpCommand(Guid OtpRequestId, string Code)
    : IRequest<Result<AuthResponse>>;

public sealed class VerifyPhoneVerifyOtpCommandValidator : AbstractValidator<VerifyPhoneVerifyOtpCommand>
{
    public VerifyPhoneVerifyOtpCommandValidator()
    {
        RuleFor(x => x.OtpRequestId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty();
    }
}

public sealed class VerifyPhoneVerifyOtpCommandHandler(
    IAppDbContext dbContext,
    IOtpService otpService,
    IUser currentUser,
    IAuthSessionFactory sessionFactory) : IRequestHandler<VerifyPhoneVerifyOtpCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(VerifyPhoneVerifyOtpCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return AuthErrors.RegistrationTokenInvalid;
        }

        var verifyResult = await otpService.VerifyAsync(
            request.OtpRequestId,
            request.Code,
            OtpChannel.Sms,
            OtpPurpose.PhoneVerify,
            cancellationToken);

        if (verifyResult.IsError)
        {
            return verifyResult.Errors;
        }

        var phone = verifyResult.Value;

        var domainUser = await dbContext.DomainUsers
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        if (domainUser is null)
        {
            return UserErrors.NotFound;
        }

        // Re-check uniqueness at commit time to close the race between request and verify.
        var verifiedElsewhere = await dbContext.DomainUsers
            .AnyAsync(u => u.Phone == phone && u.IsPhoneVerified && u.IsActive && u.Id != userId, cancellationToken);

        if (verifiedElsewhere)
        {
            return AuthErrors.PhoneAlreadyVerifiedElsewhere;
        }

        var setResult = domainUser.SetVerifiedPhone(phone);
        if (setResult.IsError)
        {
            return setResult.Errors;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await sessionFactory.CreateAsync(domainUser, ct: cancellationToken);
    }
}
