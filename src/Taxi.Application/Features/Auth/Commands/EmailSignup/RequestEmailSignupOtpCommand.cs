using FluentValidation;

using MediatR;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Auth;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Auth.Commands.EmailSignup;

/// <summary>
/// Email SIGNUP — step 1. Always sends a code (the existing-account conflict is resolved at
/// verify time, after ownership is proven).
/// </summary>
public sealed record RequestEmailSignupOtpCommand(string Email, string? DeviceId, string? IpAddress = null)
    : IRequest<Result<OtpRequestResponse>>;

public sealed class RequestEmailSignupOtpCommandValidator : AbstractValidator<RequestEmailSignupOtpCommand>
{
    public RequestEmailSignupOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(LocalizationKeys.Validation.EmailRequired)
            .EmailAddress().WithMessage(LocalizationKeys.Validation.EmailInvalid);
    }
}

public sealed class RequestEmailSignupOtpCommandHandler(IOtpService otpService)
    : IRequestHandler<RequestEmailSignupOtpCommand, Result<OtpRequestResponse>>
{
    public async Task<Result<OtpRequestResponse>> Handle(RequestEmailSignupOtpCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var result = await otpService.IssueAsync(
            OtpChannel.Email,
            OtpPurpose.EmailSignup,
            email,
            request.IpAddress,
            request.DeviceId,
            cancellationToken);

        return result.IsError ? result.Errors : OtpRequestResponse.From(result.Value);
    }
}
