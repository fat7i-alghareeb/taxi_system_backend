using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Auth.Commands.SendOtp;

public class SendOtpCommandHandler(
    IOtpService otpService,
    ISmsProvider smsProvider) : IRequestHandler<SendOtpCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        // 1. Generate OTP and Session
        var (sessionToken, code) = await otpService.GenerateOtpSessionAsync(request.Phone, cancellationToken);

        // 2. Send the OTP via SMS
        var message = $"Your Fat7i Taxi code is: {code}. Valid for 5 minutes.";
        await smsProvider.SendSmsAsync(request.Phone, message, cancellationToken);
        
        return sessionToken;
    }
}
