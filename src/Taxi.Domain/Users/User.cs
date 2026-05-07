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
        LocalizedText name,
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
    }

    public LocalizedText Name { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? ProfilePhotoUrl { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? ActiveVehicleId { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Result<User> Create(
        Guid id,
        string nameEn,
        string nameAr,
        string nameNl,
        string phone,
        string? email,
        UserRole role)
    {
        if (string.IsNullOrWhiteSpace(nameEn))
        {
            return AuthErrors.NameEnRequired;
        }

        if (string.IsNullOrWhiteSpace(nameAr))
        {
            return AuthErrors.NameArRequired;
        }

        if (string.IsNullOrWhiteSpace(nameNl))
        {
            return AuthErrors.NameNlRequired;
        }

        if (string.IsNullOrWhiteSpace(phone) || !Regex.IsMatch(phone, @"^\+?\d{7,15}$"))
        {
            return AuthErrors.PhoneRequired;
        }

        var localizedName = new LocalizedText(nameEn.Trim(), nameAr.Trim(), nameNl.Trim());

        return new User(id, localizedName, phone, email, role);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void SoftDelete() => DeletedAtUtc = DateTimeOffset.UtcNow;

    public Result<Success> AssignVehicle(Guid vehicleId)
    {
        if (Role != UserRole.Driver)
        {
            return Error.Validation(LocalizationKeys.User.NotADriver, "Only users with the Driver role can be assigned a vehicle.");
        }

        ActiveVehicleId = vehicleId;
        return Result.Success;
    }

    public void UnassignVehicle() => ActiveVehicleId = null;
}
