using Taxi.Application.Features.Identity.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string? policyName);

    Task<Result<AppUserDto>> AuthenticateAsync(string email, string password);

    Task<Result<AppUserDto>> AuthenticateByUserNameAsync(string userName, string password);

    Task<Result<AppUserDto>> GetUserByIdAsync(string userId);
    Task<Result<string>> GetOrCreateUserByPhoneAsync(string phone, string role);

    /// <summary>
    /// Creates a passwordless Identity (AppUser) for a Google/email sign-up where the
    /// domain account is keyed by a verified email (and an unverified contact phone).
    /// A random unusable password is set; these users authenticate via OTP/Google, not passwords.
    /// </summary>
    Task<Result<string>> CreatePasswordlessUserAsync(string email, string? phone, string role);

    Task<Result<string>> CreateUserAsync(string phone, string email, string password, string role);
    Task<Result<string>> CreateAdminUserAsync(string userName, string email, string password);
    Task<string?> GetUserNameAsync(string userId);
    Task<Result<Success>> ResetPasswordAsync(string userId, string newPassword);
    Task<Result<Success>> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<bool> RequiresPasswordResetAsync(string userId);
    Task<Result<Success>> ClearPasswordResetFlagAsync(string userId);
}

