using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Vehicles;

public static class VehicleErrors
{
    public static readonly Error CodeRequired = Error.Validation(
        code: LocalizationKeys.Vehicle.CodeRequired,
        description: "Vehicle type code is required.");

    public static readonly Error NameEnRequired = Error.Validation(
        code: LocalizationKeys.Vehicle.NameEnRequired,
        description: "English name is required.");

    public static readonly Error NameArRequired = Error.Validation(
        code: LocalizationKeys.Vehicle.NameArRequired,
        description: "Arabic name is required.");

    public static readonly Error NameNlRequired = Error.Validation(
        code: LocalizationKeys.Vehicle.NameNlRequired,
        description: "Dutch name is required.");

    public static readonly Error NameDeRequired = Error.Validation(
        code: LocalizationKeys.Vehicle.NameDeRequired,
        description: "German name is required.");

    public static readonly Error NamePlRequired = Error.Validation(
        code: LocalizationKeys.Vehicle.NamePlRequired,
        description: "Polish name is required.");

    public static readonly Error NameUkRequired = Error.Validation(
        code: LocalizationKeys.Vehicle.NameUkRequired,
        description: "Ukrainian name is required.");

    public static readonly Error NameFrRequired = Error.Validation(
        code: LocalizationKeys.Vehicle.NameFrRequired,
        description: "French name is required.");

    public static readonly Error NameEsRequired = Error.Validation(
        code: LocalizationKeys.Vehicle.NameEsRequired,
        description: "Spanish name is required.");

    public static readonly Error NameRoRequired = Error.Validation(
        code: LocalizationKeys.Vehicle.NameRoRequired,
        description: "Romanian name is required.");

    public static readonly Error NotFound = Error.NotFound(
        code: LocalizationKeys.Vehicle.NotFound,
        description: "Vehicle not found.");

    public static readonly Error DriverNotFound = Error.NotFound(
        code: LocalizationKeys.Vehicle.DriverNotFound,
        description: "Driver not found or is not a driver.");
}

