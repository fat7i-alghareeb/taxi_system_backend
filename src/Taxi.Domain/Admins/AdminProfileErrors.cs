using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Admins;

public static class AdminProfileErrors
{
    public static readonly Error NameRequired = Error.Validation(
        code: LocalizationKeys.AdminProfile.NameRequired,
        description: "Admin name is required.");

    public static readonly Error EmailRequired = Error.Validation(
        code: LocalizationKeys.AdminProfile.EmailRequired,
        description: "Admin email is required.");

    public static readonly Error EmailInvalid = Error.Validation(
        code: LocalizationKeys.AdminProfile.EmailInvalid,
        description: "Admin email is invalid.");

    public static readonly Error PhoneInvalid = Error.Validation(
        code: LocalizationKeys.AdminProfile.PhoneInvalid,
        description: "Admin phone number is invalid.");

    public static readonly Error NotFound = Error.NotFound(
        code: LocalizationKeys.AdminProfile.NotFound,
        description: "Admin profile not found.");
}
