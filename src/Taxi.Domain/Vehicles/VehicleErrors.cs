using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Vehicles;

public static class VehicleErrors
{
    public static readonly Error CodeRequired = Error.Validation(
        code: "Vehicle.CodeRequired",
        description: "Vehicle type code is required.");

    public static readonly Error NameEnRequired = Error.Validation(
        code: "Vehicle.NameEnRequired",
        description: "English name is required.");

    public static readonly Error NameArRequired = Error.Validation(
        code: "Vehicle.NameArRequired",
        description: "Arabic name is required.");

    public static readonly Error NameNlRequired = Error.Validation(
        code: "Vehicle.NameNlRequired",
        description: "Dutch name is required.");

    public static readonly Error NotFound = Error.NotFound(
        code: "Vehicle.NotFound",
        description: "Vehicle not found.");

    public static readonly Error DriverNotFound = Error.NotFound(
        code: "Vehicle.DriverNotFound",
        description: "Driver not found or is not a driver.");
}

