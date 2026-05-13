using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Auth.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IFirebaseAuthService firebaseAuth,
    IIdentityService identityService,
    ITokenProvider tokenProvider,
    IAppDbContext dbContext,
    ILanguageContext languageContext) : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify Firebase ID token, extract verified phone
        var verifyResult = await firebaseAuth.VerifyIdTokenAndGetPhoneAsync(request.FirebaseIdToken, cancellationToken);
        if (verifyResult.IsError)
        {
            return verifyResult.Errors;
        }

        var verifiedPhone = NormalizePhone(verifyResult.Value);
        if (NormalizePhone(request.Phone) != verifiedPhone)
        {
            return AuthErrors.PhoneMismatch;
        }

        // 2. Resolve or create the Identity (AppUser) using the verified phone
        var identityResult = await identityService.GetOrCreateUserByPhoneAsync(verifiedPhone, UserRole.Passenger.ToString());
        if (identityResult.IsError)
        {
            return identityResult.Errors;
        }

        var identityId = identityResult.Value;

        // 3. Resolve or create the Domain User (silent registration with trilingual placeholders)
        var domainUser = await dbContext.DomainUsers
            .FirstOrDefaultAsync(u => u.Phone == verifiedPhone, cancellationToken);

        if (domainUser is null)
        {
            var placeholderEn = $"Passenger {verifiedPhone}";
            var placeholderAr = $"راكب {verifiedPhone}";
            var placeholderNl = $"Passagier {verifiedPhone}";
            var placeholderDe = $"Passagier {verifiedPhone}";
            var placeholderPl = $"Pasażer {verifiedPhone}";
            var placeholderUk = $"Пасажир {verifiedPhone}";
            var placeholderFr = $"Passager {verifiedPhone}";
            var placeholderEs = $"Pasajero {verifiedPhone}";
            var placeholderRo = $"Pasager {verifiedPhone}";

            var createResult = User.Create(
                Guid.Parse(identityId),
                placeholderEn,
                placeholderAr,
                placeholderNl,
                placeholderDe,
                placeholderPl,
                placeholderUk,
                placeholderFr,
                placeholderEs,
                placeholderRo,
                verifiedPhone,
                null,
                UserRole.Passenger);

            if (createResult.IsError)
            {
                return createResult.Errors;
            }

            domainUser = createResult.Value;
            dbContext.DomainUsers.Add(domainUser);
        }
        else if (!domainUser.IsActive)
        {
            return AuthErrors.UserInactive;
        }

        // 4. Persist FCM token if provided (best-effort: client may omit it)
        if (!string.IsNullOrWhiteSpace(request.FcmToken))
        {
            var fcmResult = domainUser.UpdateFcmToken(request.FcmToken);
            if (fcmResult.IsError)
            {
                return fcmResult.Errors;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 5. Resolve Identity DTO for token generation
        var appUserResult = await identityService.GetUserByIdAsync(identityId);
        if (appUserResult.IsError)
        {
            return appUserResult.Errors;
        }

        // 6. Issue JWT
        var tokenResult = await tokenProvider.GenerateJwtTokenAsync(appUserResult.Value, cancellationToken);
        if (tokenResult.IsError)
        {
            return tokenResult.Errors;
        }

        // 7. Build response (hide placeholder names so the client treats them as null)
        var resolvedName = domainUser.Name.GetTranslation(languageContext.Language);
        var isPlaceholder = resolvedName.StartsWith("Passenger ")
            || resolvedName.StartsWith("راكب ")
            || resolvedName.StartsWith("Passagier ")
            || resolvedName.StartsWith("Pasażer ")
            || resolvedName.StartsWith("Пасажир ")
            || resolvedName.StartsWith("Passager ")
            || resolvedName.StartsWith("Pasajero ")
            || resolvedName.StartsWith("Pasager ");

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

    private static string NormalizePhone(string phone) =>
        phone.Trim().Replace(" ", string.Empty);
}
