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
    IAppDbContext dbContext) : IRequestHandler<LoginCommand, Result<AuthResponse>>
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

        // 3. Resolve or create the Domain User (silent registration with trilingual placeholders).
        // IgnoreQueryFilters so a previously soft-deleted account is found and can be revived,
        // otherwise re-registration would try to insert a duplicate Id/Phone and fail.
        var placeholder = $"Passenger {verifiedPhone}";

        var domainUser = await dbContext.DomainUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Phone == verifiedPhone, cancellationToken);

        if (domainUser is null)
        {
            var createResult = User.Create(
                Guid.Parse(identityId),
                placeholder,
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
        else if (domainUser.DeletedAtUtc is not null)
        {
            // Self-deleted account re-registering: revive it as a fresh blank profile.
            var reviveResult = domainUser.ReviveForReRegistration(placeholder);
            if (reviveResult.IsError)
            {
                return reviveResult.Errors;
            }
        }
        else if (!domainUser.IsActive)
        {
            // Admin-deactivated (banned) account; distinct from self-deletion.
            return UserErrors.Inactive;
        }

        // 4. Persist FCM token if provided (best-effort: client may omit it)
        if (!string.IsNullOrWhiteSpace(request.FcmToken))
        {
            // Override: this device token must belong only to the account logging in now.
            // Clear it from any other account that previously registered it on this device,
            // so stale users stop receiving push notifications meant for them.
            var previousOwners = await dbContext.DomainUsers
                .IgnoreQueryFilters()
                .Where(u => u.FcmToken == request.FcmToken && u.Id != domainUser.Id)
                .ToListAsync(cancellationToken);

            foreach (var previousOwner in previousOwners)
            {
                previousOwner.UpdateFcmToken(null);
            }

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
        var resolvedName = domainUser.Name;
        var isPlaceholder = resolvedName.StartsWith("Passenger ");

        Guid? driverId = null;
        string? approvalStatus = null;

        if (domainUser.Role == UserRole.Driver)
        {
            var driver = await dbContext.Drivers.FirstOrDefaultAsync(d => d.UserId == domainUser.Id, cancellationToken);
            if (driver != null)
            {
                driverId = driver.Id;
                approvalStatus = driver.ApprovalStatus.ToString();
            }
        }

        var userDto = new UserDto
        {
            Id = domainUser.Id,
            Phone = domainUser.Phone,
            Role = domainUser.Role.ToString(),
            Email = domainUser.Email,
            ProfilePhotoUrl = domainUser.ProfilePhotoUrl,
            Name = isPlaceholder ? null : resolvedName,
            DriverId = driverId,
            ApprovalStatus = approvalStatus,
        };

        return new AuthResponse(
            tokenResult.Value.AccessToken,
            tokenResult.Value.RefreshToken,
            userDto);
    }

    private static string NormalizePhone(string phone) =>
        phone.Trim().Replace(" ", string.Empty);
}
