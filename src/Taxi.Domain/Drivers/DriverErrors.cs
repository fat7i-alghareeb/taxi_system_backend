using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Drivers;

public static class DriverErrors
{
    public static readonly Error UserIdRequired = Error.Validation(
        code: "Driver.UserIdRequired",
        description: "User ID is required.");

    public static readonly Error LicenseRequired = Error.Validation(
        code: "Driver.LicenseRequired",
        description: "License number is required.");

    public static readonly Error NotFound = Error.NotFound(
        code: "Driver.NotFound",
        description: "Driver not found.");
}

