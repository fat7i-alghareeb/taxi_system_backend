using FluentValidation;

using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.PhoneSignup;

/// <summary>
/// Phone SIGNUP — step 1. Always sends a code (existence is resolved at verify time so the
/// existing-account conflict can be surfaced only after ownership is proven).
/// </summary>
public sealed record RequestPhoneSignupOtpCommand(string Phone, string? DeviceId, string? IpAddress = null)
    : IRequest<Result<OtpRequestResponse>>;

public sealed class RequestPhoneSignupOtpCommandValidator : AbstractValidator<RequestPhoneSignupOtpCommand>
{
    public RequestPhoneSignupOtpCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage(LocalizationKeys.User.PhoneRequired);
    }
}

public sealed class RequestPhoneSignupOtpCommandHandler(IOtpService otpService)
    : IRequestHandler<RequestPhoneSignupOtpCommand, Result<OtpRequestResponse>>
{
    public async Task<Result<OtpRequestResponse>> Handle(RequestPhoneSignupOtpCommand request, CancellationToken cancellationToken)
    {
        var phone = PhoneNumberNormalizer.Normalize(request.Phone);

        var result = await otpService.IssueAsync(
            OtpChannel.Sms,
            OtpPurpose.PhoneSignup,
            phone,
            request.IpAddress,
            request.DeviceId,
            cancellationToken);

        return result.IsError ? result.Errors : OtpRequestResponse.From(result.Value);
    }
}
