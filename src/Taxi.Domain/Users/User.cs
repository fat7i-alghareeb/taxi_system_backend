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

    /// <summary>
    /// The passenger's preferred payment-method TYPE for new trip bookings (e.g. "card",
    /// "ideal", "apple_pay"). Metadata only — it does not drive automatic extra-fee charging.
    /// Null means "no preference / ask each time".
    /// </summary>
    public string? PreferredPaymentMethodType { get; private set; }

    public HomeAddress? HomeAddress { get; private set; }

    /// <summary>
    /// Whether <see cref="Phone"/> has been proven via SMS OTP. Only verified phones are
    /// unique (partial index) and usable for phone login. Phone entered during Google/email
    /// sign-up is stored unverified until the user completes the "Verify now" flow.
    /// </summary>
    public bool IsPhoneVerified { get; private set; }

    /// <summary>
    /// Whether <see cref="Email"/> has been proven (email OTP or a provider-verified Google
    /// email). Only verified emails are unique (partial index) and usable for email login.
    /// </summary>
    public bool IsEmailVerified { get; private set; }

    /// <summary>Firebase/Google account uid used to identify a returning Google user.</summary>
    public string? GoogleId { get; private set; }

    /// <summary>
    /// Set when the user chooses "start fresh". Customer-facing trip history is hidden before
    /// this instant; admin/accounting/payment/invoice queries ignore it and keep all rows.
    /// </summary>
    public DateTimeOffset? ProfileResetAtUtc { get; private set; }

    public static Result<User> Create(
        Guid id,
        string name,
        string phone,
        string? email,
        UserRole role,
        bool isPhoneVerified = true,
        bool isEmailVerified = false,
        string? googleId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return UserErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, @"^\+?\d{7,15}$"))
        {
            return UserErrors.PhoneRequired;
        }

        var trimmedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        return new User(id, name.Trim(), phone.Trim(), trimmedEmail, role)
        {
            IsPhoneVerified = isPhoneVerified,
            IsEmailVerified = trimmedEmail is not null && isEmailVerified,
            GoogleId = string.IsNullOrWhiteSpace(googleId) ? null : googleId.Trim(),
        };
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

    /// <summary>
    /// Revives a previously soft-deleted account so the same phone can register again,
    /// resetting personal profile data to a fresh state. Trip history rows are kept in the
    /// database for legal/accounting purposes and are not touched here.
    /// </summary>
    /// <param name="placeholderName">Placeholder name used until the user sets a real one.</param>
    public Result<Success> ReviveForReRegistration(string placeholderName)
    {
        if (string.IsNullOrWhiteSpace(placeholderName))
        {
            return UserErrors.NameRequired;
        }

        DeletedAtUtc = null;
        IsActive = true;
        Name = placeholderName.Trim();
        Email = null;
        IsEmailVerified = false;
        GoogleId = null;
        ProfilePhotoUrl = null;
        FcmToken = null;
        HomeAddress = null;
        // Re-registration happens through the verified phone flow, so the phone stays verified.
        IsPhoneVerified = true;
        ProfileResetAtUtc = null;
        return Result.Success;
    }

    /// <summary>Marks the current phone as SMS-verified (unique-index eligible, login-eligible).</summary>
    public Result<Success> MarkPhoneVerified()
    {
        IsPhoneVerified = true;
        return Result.Success;
    }

    /// <summary>Marks the current email as verified (unique-index eligible, login-eligible).</summary>
    public Result<Success> MarkEmailVerified()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            return UserErrors.EmailRequired;
        }

        IsEmailVerified = true;
        return Result.Success;
    }

    /// <summary>
    /// Sets (or replaces) the phone and marks it verified. Used by the authorized
    /// "verify now" flow; uniqueness against other verified accounts is enforced by the
    /// caller before this is invoked.
    /// </summary>
    public Result<Success> SetVerifiedPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, @"^\+?\d{7,15}$"))
        {
            return UserErrors.PhoneRequired;
        }

        Phone = phone.Trim();
        IsPhoneVerified = true;
        return Result.Success;
    }

    public Result<Success> LinkGoogle(string googleId)
    {
        if (string.IsNullOrWhiteSpace(googleId))
        {
            return UserErrors.GoogleIdRequired;
        }

        GoogleId = googleId.Trim();
        return Result.Success;
    }

    /// <summary>
    /// "Start fresh": wipes personal profile data and stamps <see cref="ProfileResetAtUtc"/>
    /// so the user stops seeing pre-reset history, while the account id, phone verification
    /// and all historical rows (trips/invoices) are preserved for admin/accounting. No hard delete.
    /// </summary>
    public Result<Success> ResetForFreshStart(string placeholderName, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(placeholderName))
        {
            return UserErrors.NameRequired;
        }

        Name = placeholderName.Trim();
        Email = null;
        IsEmailVerified = false;
        ProfilePhotoUrl = null;
        HomeAddress = null;
        ProfileResetAtUtc = nowUtc;
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
            var newEmail = trimmed.Length == 0 ? null : trimmed;

            // Changing the email drops its verified status: a self-edited address is
            // contact data until re-proven through the email OTP flow.
            if (!string.Equals(newEmail, Email, StringComparison.OrdinalIgnoreCase))
            {
                IsEmailVerified = false;
            }

            Email = newEmail;
        }

        return Result.Success;
    }

    /// <summary>
    /// Sets or clears the optional home address. Passing all-null/blank values clears it.
    /// When provided, a non-blank label and in-range coordinates are required.
    /// </summary>
    public Result<Success> UpdateHomeAddress(string? label, decimal? latitude, decimal? longitude)
    {
        if (string.IsNullOrWhiteSpace(label) && latitude is null && longitude is null)
        {
            HomeAddress = null;
            return Result.Success;
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return UserErrors.HomeAddressInvalid;
        }

        if (latitude.HasValue != longitude.HasValue)
        {
            return UserErrors.HomeAddressInvalid;
        }

        if (latitude is < -90m or > 90m || longitude is < -180m or > 180m)
        {
            return UserErrors.HomeAddressInvalid;
        }

        HomeAddress = new HomeAddress(label.Trim(), latitude, longitude);
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

    /// <summary>
    /// Sets (or clears, when null/blank) the preferred trip payment-method type. The value is
    /// normalized to lower-case; callers validate it against the enabled method types.
    /// </summary>
    public Result<Success> SetPreferredPaymentMethodType(string? methodType)
    {
        PreferredPaymentMethodType = string.IsNullOrWhiteSpace(methodType)
            ? null
            : methodType.Trim().ToLowerInvariant();
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
