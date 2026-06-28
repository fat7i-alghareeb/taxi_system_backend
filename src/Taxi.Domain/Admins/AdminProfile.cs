using System.Text.RegularExpressions;
using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Users;

namespace Taxi.Domain.Admins;

public sealed class AdminProfile : AuditableEntity
{
    private AdminProfile()
    {
    }

    private AdminProfile(
        Guid id,
        string name,
        string email,
        string? phone1,
        string? phone2)
        : base(id)
    {
        Name = name;
        Email = email;
        Phone1 = phone1;
        Phone2 = phone2;
        IsActive = true;
    }

    public string Name { get; private set; } = default!;

    public string Email { get; private set; } = default!;

    public string? Phone1 { get; private set; }

    public string? Phone2 { get; private set; }

    public bool IsActive { get; private set; }

    public string? FcmToken { get; private set; }

    public string PreferredLanguage { get; private set; } = "en";

    public static Result<AdminProfile> Create(
        Guid id,
        string name,
        string email,
        string? phone1 = null,
        string? phone2 = null)
    {
        var validationResult = Validate(name, email, phone1, phone2);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }

        return new AdminProfile(
            id,
            name.Trim(),
            email.Trim(),
            NormalizeOptionalPhone(phone1),
            NormalizeOptionalPhone(phone2));
    }

    public Result<Success> Update(
        string name,
        string email,
        string? phone1,
        string? phone2)
    {
        var validationResult = Validate(name, email, phone1, phone2);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }

        Name = name.Trim();
        Email = email.Trim();
        Phone1 = NormalizeOptionalPhone(phone1);
        Phone2 = NormalizeOptionalPhone(phone2);

        return Result.Success;
    }

    public Result<Success> UpdateFcmToken(string? fcmToken)
    {
        if (fcmToken?.Length > 4096)
        {
            return UserErrors.FcmTokenInvalid;
        }

        FcmToken = string.IsNullOrWhiteSpace(fcmToken) ? null : fcmToken.Trim();
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

    private static Result<Success> Validate(string name, string email, string? phone1, string? phone2)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return AdminProfileErrors.NameRequired;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return AdminProfileErrors.EmailRequired;
        }

        if (!email.Contains('@', StringComparison.Ordinal))
        {
            return AdminProfileErrors.EmailInvalid;
        }

        if (!IsValidOptionalPhone(phone1) || !IsValidOptionalPhone(phone2))
        {
            return AdminProfileErrors.PhoneInvalid;
        }

        return Result.Success;
    }

    private static bool IsValidOptionalPhone(string? phone)
    {
        return string.IsNullOrWhiteSpace(phone) || Regex.IsMatch(phone.Trim(), @"^\+?\d{7,15}$");
    }

    private static string? NormalizeOptionalPhone(string? phone)
    {
        return string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
}
