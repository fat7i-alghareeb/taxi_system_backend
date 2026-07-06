using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.PhoneLogin;

/// <summary>
/// Phone LOGIN — step 1. Requires an existing account whose phone is verified; otherwise
/// returns "No account found" and sends no SMS. <c>IpAddress</c> is set by the controller.
/// </summary>
public sealed record RequestPhoneLoginOtpCommand(string Phone, string? DeviceId, string? IpAddress = null)
    : IRequest<Result<OtpRequestResponse>>;

public sealed class RequestPhoneLoginOtpCommandValidator : AbstractValidator<RequestPhoneLoginOtpCommand>
{
    public RequestPhoneLoginOtpCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage(LocalizationKeys.User.PhoneRequired);
    }
}

public sealed class RequestPhoneLoginOtpCommandHandler(IAppDbContext dbContext, IOtpService otpService)
    : IRequestHandler<RequestPhoneLoginOtpCommand, Result<OtpRequestResponse>>
{
    public async Task<Result<OtpRequestResponse>> Handle(RequestPhoneLoginOtpCommand request, CancellationToken cancellationToken)
    {
        var phone = PhoneNumberNormalizer.Normalize(request.Phone);

        var accountExists = await dbContext.DomainUsers
            .AnyAsync(u => u.Phone == phone && u.IsPhoneVerified && u.IsActive, cancellationToken);

        if (!accountExists)
        {
            return AuthErrors.NoAccountFound;
        }

        var result = await otpService.IssueAsync(
            OtpChannel.Sms,
            OtpPurpose.PhoneLogin,
            phone,
            request.IpAddress,
            request.DeviceId,
            cancellationToken);

        return result.IsError ? result.Errors : OtpRequestResponse.From(result.Value);
    }
}
