using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.VerifyOtp;

public class VerifyOtpCommandHandler(
    IOtpService otpService,
    IIdentityService identityService,
    ITokenProvider tokenProvider,
    IAppDbContext dbContext,
    ILanguageContext languageContext) : IRequestHandler<VerifyOtpCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify the OTP
        var verifyResult = await otpService.VerifyOtpAsync(request.SessionToken, request.Code, cancellationToken);
        if (verifyResult.IsError)
        {
            return verifyResult.Errors;
        }

        // 2. Resolve or Create the Identity (AppUser)
        var identityResult = await identityService.GetOrCreateUserByPhoneAsync(request.Phone, UserRole.Passenger.ToString());
        if (identityResult.IsError)
        {
            return identityResult.Errors;
        }

        var appUser = identityResult.Value;

        // 3. Resolve or Create the Domain User
        var domainUser = await dbContext.DomainUsers
            .FirstOrDefaultAsync(u => u.Phone == request.Phone, cancellationToken);

        if (domainUser == null)
        {
            // Silent Registration - Use placeholders for trilingual name
            var placeholderEn = $"Passenger {request.Phone}";
            var placeholderAr = $"راكب {request.Phone}";
            var placeholderNl = $"Passagier {request.Phone}";

            var createResult = User.Create(
                Guid.Parse(appUser.UserId),
                placeholderEn,
                placeholderAr,
                placeholderNl,
                request.Phone,
                null,
                UserRole.Passenger);

            if (createResult.IsError)
            {
                return createResult.Errors;
            }

            domainUser = createResult.Value;
            dbContext.DomainUsers.Add(domainUser);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (!domainUser.IsActive)
        {
            return AuthErrors.UserInactive;
        }

        // 4. Issue Tokens
        var tokenResult = await tokenProvider.GenerateJwtTokenAsync(appUser, cancellationToken);
        if (tokenResult.IsError)
        {
            return tokenResult.Errors;
        }

        // 5. Prepare Response
        var resolvedName = domainUser.Name.GetTranslation(languageContext.Language);
        var isPlaceholder = resolvedName.StartsWith("Passenger ") || resolvedName.StartsWith("راكب ") || resolvedName.StartsWith("Passagier ");

        var userDto = new UserDto
        {
            Id = domainUser.Id,
            Phone = domainUser.Phone,
            Role = domainUser.Role.ToString(),
            Email = domainUser.Email,
            ProfilePhotoUrl = domainUser.ProfilePhotoUrl,
            Name = isPlaceholder ? null : resolvedName,
        };

        return new AuthResponse(
            tokenResult.Value.AccessToken,
            tokenResult.Value.RefreshToken,
            userDto);
    }
}
