using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Drivers;

public static class DriverErrors
{
    public static readonly Error UserIdRequired = Error.Validation(
        code: LocalizationKeys.Driver.UserIdRequired,
        description: "User ID is required.");

    public static readonly Error LicenseRequired = Error.Validation(
        code: LocalizationKeys.Driver.LicenseRequired,
        description: "License number is required.");

    public static readonly Error NotFound = Error.NotFound(
        code: LocalizationKeys.Driver.NotFound,
        description: "Driver not found.");
}
