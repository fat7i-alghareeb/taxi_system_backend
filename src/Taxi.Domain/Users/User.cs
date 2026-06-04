using System.Text.RegularExpressions;
using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Users;

public sealed class User : AuditableEntity
{
    private User() { } // EF Core

    private User(
        Guid id,
        string name,
        string phone,
        string? email,
        UserRole role)
        : base(id)
    {
        Name = name;
        Phone = phone;
        Email = email;
        Role = role;
        IsActive = true;
        PreferredLanguage = "en";
    }

    public string Name { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? ProfilePhotoUrl { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public string? FcmToken { get; private set; }
    public string PreferredLanguage { get; private set; } = "en";
    public string? StripeCustomerId { get; private set; }

    public static Result<User> Create(
        Guid id,
        string name,
        string phone,
        string? email,
        UserRole role)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return UserErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, @"^\+?\d{7,15}$"))
        {
            return UserErrors.PhoneRequired;
        }

        return new User(id, name.Trim(), phone, email, role);
    }

    /// <summary>
    /// Creates an admin user without requiring a phone number.
    /// Admins authenticate by username + password, not by phone.
    /// </summary>
    /// <param name="id">The unique identifier for the admin user.</param>
    /// <param name="name">The name of the admin user.</param>
    /// <param name="phone">The phone number of the admin user.</param>
    /// <param name="email">The email address of the admin user (optional).</param>
    public static Result<User> CreateAdmin(
        Guid id,
        string name,
        string phone,
        string? email = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return UserErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, @"^\+?\d{7,15}$"))
        {
            return UserErrors.PhoneRequired;
        }

        return new User(id, name.Trim(), phone.Trim(), email, UserRole.Admin);
    }

    public Result<Success> Deactivate()
    {
        IsActive = false;
        return Result.Success;
    }

    public Result<Success> Activate()
    {
        IsActive = true;
        return Result.Success;
    }

    public Result<Success> SoftDelete()
    {
        if (DeletedAtUtc.HasValue)
        {
            return Result.Success;
        }

        DeletedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success;
    }

    public Result<Success> UpdateProfile(string name, string? profilePhotoUrl = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return UserErrors.ProfileNameRequired;
        }

        Name = name.Trim();

        if (profilePhotoUrl is not null)
        {
            ProfilePhotoUrl = profilePhotoUrl;
        }

        if (email is not null)
        {
            var trimmed = email.Trim();
            Email = trimmed.Length == 0 ? null : trimmed;
        }

        return Result.Success;
    }

    public Result<Success> UpdateFcmToken(string? fcmToken)
    {
        if (fcmToken is { Length: > 4096 })
        {
            return UserErrors.FcmTokenInvalid;
        }

        FcmToken = string.IsNullOrWhiteSpace(fcmToken) ? null : fcmToken.Trim();
        return Result.Success;
    }

    public Result<Success> SetStripeCustomerId(string stripeCustomerId)
    {
        if (string.IsNullOrWhiteSpace(stripeCustomerId))
        {
            return UserErrors.StripeCustomerIdRequired;
        }

        StripeCustomerId = stripeCustomerId.Trim();
        return Result.Success;
    }

    public Result<Success> UpdatePreferredLanguage(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return UserErrors.PreferredLanguageRequired;
        }

        var normalized = languageCode.Trim().ToLower();
        if (normalized != "en" && normalized != "ar" && normalized != "nl" && normalized != "de" && normalized != "pl" && normalized != "uk" && normalized != "fr" && normalized != "es" && normalized != "ro")
        {
            return UserErrors.PreferredLanguageInvalid;
        }

        PreferredLanguage = normalized;
        return Result.Success;
    }
}
