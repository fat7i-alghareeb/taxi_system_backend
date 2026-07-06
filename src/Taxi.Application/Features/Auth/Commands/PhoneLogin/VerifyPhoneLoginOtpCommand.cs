using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.Features.Auth.Services;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.PhoneLogin;

/// <summary>
/// Phone LOGIN — step 2. Verifies the code and returns the existing account's session.
/// Never silently creates an account (that is the signup flow's job).
/// </summary>
public sealed record VerifyPhoneLoginOtpCommand(Guid OtpRequestId, string Code, string? FcmToken)
    : IRequest<Result<AuthResponse>>;

public sealed class VerifyPhoneLoginOtpCommandValidator : AbstractValidator<VerifyPhoneLoginOtpCommand>
{
    public VerifyPhoneLoginOtpCommandValidator()
    {
        RuleFor(x => x.OtpRequestId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty();
    }
}

public sealed class VerifyPhoneLoginOtpCommandHandler(
    IAppDbContext dbContext,
    IOtpService otpService,
    IAuthSessionFactory sessionFactory) : IRequestHandler<VerifyPhoneLoginOtpCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(VerifyPhoneLoginOtpCommand request, CancellationToken cancellationToken)
    {
        var verifyResult = await otpService.VerifyAsync(
            request.OtpRequestId,
            request.Code,
            OtpChannel.Sms,
            OtpPurpose.PhoneLogin,
            cancellationToken);

        if (verifyResult.IsError)
        {
            return verifyResult.Errors;
        }

        var phone = verifyResult.Value;

        var domainUser = await dbContext.DomainUsers
            .FirstOrDefaultAsync(u => u.Phone == phone && u.IsPhoneVerified && u.IsActive, cancellationToken);

        if (domainUser is null)
        {
            return AuthErrors.NoAccountFound;
        }

        await FcmTokenSync.ApplyAsync(dbContext, domainUser, request.FcmToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await sessionFactory.CreateAsync(domainUser, ct: cancellationToken);
    }
}
