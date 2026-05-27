using System.Text.RegularExpressions;
using Taxi.Contracts.Common;
using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

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

    private static Result<Success> Validate(string name, string email, string? phone1, string? phone2)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation(LocalizationKeys.User.NameRequired, "Name is required.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            return Error.Validation(LocalizationKeys.Validation.EmailRequired, "Email is required.");
        }

        if (!IsValidOptionalPhone(phone1) || !IsValidOptionalPhone(phone2))
        {
            return Error.Validation(LocalizationKeys.User.PhoneRequired, "Phone number is invalid.");
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
