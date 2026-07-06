using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Application.Features.Auth.Services;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.EmailLogin;

/// <summary>Email LOGIN — step 2. Verifies the code and returns the verified-email account's session.</summary>
public sealed record VerifyEmailLoginOtpCommand(Guid OtpRequestId, string Code, string? FcmToken)
    : IRequest<Result<AuthResponse>>;

public sealed class VerifyEmailLoginOtpCommandValidator : AbstractValidator<VerifyEmailLoginOtpCommand>
{
    public VerifyEmailLoginOtpCommandValidator()
    {
        RuleFor(x => x.OtpRequestId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty();
    }
}

public sealed class VerifyEmailLoginOtpCommandHandler(
    IAppDbContext dbContext,
    IOtpService otpService,
    IAuthSessionFactory sessionFactory) : IRequestHandler<VerifyEmailLoginOtpCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(VerifyEmailLoginOtpCommand request, CancellationToken cancellationToken)
    {
        var verifyResult = await otpService.VerifyAsync(
            request.OtpRequestId,
            request.Code,
            OtpChannel.Email,
            OtpPurpose.EmailLogin,
            cancellationToken);

        if (verifyResult.IsError)
        {
            return verifyResult.Errors;
        }

        var email = verifyResult.Value;

        var domainUser = await dbContext.DomainUsers
            .FirstOrDefaultAsync(
                u => u.Email != null && u.Email.ToLower() == email && u.IsEmailVerified && u.IsActive,
                cancellationToken);

        if (domainUser is null)
        {
            return AuthErrors.NoAccountFound;
        }

        await FcmTokenSync.ApplyAsync(dbContext, domainUser, request.FcmToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await sessionFactory.CreateAsync(domainUser, ct: cancellationToken);
    }
}
