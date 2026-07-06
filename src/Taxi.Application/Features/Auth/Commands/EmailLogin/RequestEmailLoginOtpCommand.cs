using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.EmailLogin;

/// <summary>
/// Email LOGIN — step 1. Requires an account with a VERIFIED email; otherwise returns
/// "No account found" and sends no email.
/// </summary>
public sealed record RequestEmailLoginOtpCommand(string Email, string? DeviceId, string? IpAddress = null)
    : IRequest<Result<OtpRequestResponse>>;

public sealed class RequestEmailLoginOtpCommandValidator : AbstractValidator<RequestEmailLoginOtpCommand>
{
    public RequestEmailLoginOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(LocalizationKeys.Validation.EmailRequired)
            .EmailAddress().WithMessage(LocalizationKeys.Validation.EmailInvalid);
    }
}

public sealed class RequestEmailLoginOtpCommandHandler(IAppDbContext dbContext, IOtpService otpService)
    : IRequestHandler<RequestEmailLoginOtpCommand, Result<OtpRequestResponse>>
{
    public async Task<Result<OtpRequestResponse>> Handle(RequestEmailLoginOtpCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var accountExists = await dbContext.DomainUsers
            .AnyAsync(u => u.Email != null && u.Email.ToLower() == email && u.IsEmailVerified && u.IsActive, cancellationToken);

        if (!accountExists)
        {
            return AuthErrors.NoAccountFound;
        }

        var result = await otpService.IssueAsync(
            OtpChannel.Email,
            OtpPurpose.EmailLogin,
            email,
            request.IpAddress,
            request.DeviceId,
            cancellationToken);

        return result.IsError ? result.Errors : OtpRequestResponse.From(result.Value);
    }
}
