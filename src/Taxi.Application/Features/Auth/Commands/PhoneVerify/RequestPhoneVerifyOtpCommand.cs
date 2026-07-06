using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.PhoneVerify;

/// <summary>
/// Authorized "Verify now" — step 1. Sends an SMS code to verify/claim a phone for the
/// current account. If the phone is already VERIFIED on another active account, returns a
/// conflict and sends nothing (the real owner already holds it).
/// </summary>
public sealed record RequestPhoneVerifyOtpCommand(string Phone, string? DeviceId, string? IpAddress = null)
    : IRequest<Result<OtpRequestResponse>>;

public sealed class RequestPhoneVerifyOtpCommandValidator : AbstractValidator<RequestPhoneVerifyOtpCommand>
{
    public RequestPhoneVerifyOtpCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage(LocalizationKeys.User.PhoneRequired);
    }
}

public sealed class RequestPhoneVerifyOtpCommandHandler(
    IAppDbContext dbContext,
    IOtpService otpService,
    IUser currentUser) : IRequestHandler<RequestPhoneVerifyOtpCommand, Result<OtpRequestResponse>>
{
    public async Task<Result<OtpRequestResponse>> Handle(RequestPhoneVerifyOtpCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return AuthErrors.RegistrationTokenInvalid;
        }

        var phone = PhoneNumberNormalizer.Normalize(request.Phone);

        var verifiedElsewhere = await dbContext.DomainUsers
            .AnyAsync(u => u.Phone == phone && u.IsPhoneVerified && u.IsActive && u.Id != userId, cancellationToken);

        if (verifiedElsewhere)
        {
            return AuthErrors.PhoneAlreadyVerifiedElsewhere;
        }

        var result = await otpService.IssueAsync(
            OtpChannel.Sms,
            OtpPurpose.PhoneVerify,
            phone,
            request.IpAddress,
            request.DeviceId,
            cancellationToken);

        return result.IsError ? result.Errors : OtpRequestResponse.From(result.Value);
    }
}
