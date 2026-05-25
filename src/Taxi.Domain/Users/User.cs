using System.Text.RegularExpressions;
using Taxi.Contracts.Common;
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
            return AuthErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, @"^\+?\d{7,15}$"))
        {
            return AuthErrors.PhoneRequired;
        }

        return new User(id, name.Trim(), phone, email, role);
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
        DeletedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success;
    }

    public Result<Success> UpdateProfile(string name, string? profilePhotoUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation(LocalizationKeys.User.ProfileNameRequired, "Name is required.");
        }

        Name = name.Trim();

        if (profilePhotoUrl is not null)
        {
            ProfilePhotoUrl = profilePhotoUrl;
        }

        return Result.Success;
    }

    public Result<Success> UpdateFcmToken(string? fcmToken)
    {
        if (fcmToken is { Length: > 4096 })
        {
            return Error.Validation(LocalizationKeys.User.FcmTokenInvalid, "FCM token is invalid.");
        }

        FcmToken = string.IsNullOrWhiteSpace(fcmToken) ? null : fcmToken.Trim();
        return Result.Success;
    }

    public Result<Success> SetStripeCustomerId(string stripeCustomerId)
    {
        if (string.IsNullOrWhiteSpace(stripeCustomerId))
        {
            return Error.Validation(LocalizationKeys.User.StripeCustomerIdRequired, "Stripe customer id is required.");
        }

        StripeCustomerId = stripeCustomerId.Trim();
        return Result.Success;
    }

    public Result<Success> UpdatePreferredLanguage(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return Error.Validation(LocalizationKeys.User.PreferredLanguageRequired, "Preferred language is required.");
        }

        var normalized = languageCode.Trim().ToLower();
        if (normalized != "en" && normalized != "ar" && normalized != "nl" && normalized != "de" && normalized != "pl" && normalized != "uk" && normalized != "fr" && normalized != "es" && normalized != "ro")
        {
            return Error.Validation(LocalizationKeys.User.PreferredLanguageInvalid, "Preferred language is invalid.");
        }

        PreferredLanguage = normalized;
        return Result.Success;
    }
}

