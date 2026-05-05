using Taxi.Contracts.Common;
using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Vehicles;

public sealed class Vehicle : AuditableEntity
{
    private Vehicle() { } // EF Core

    private Vehicle(
        Guid id,
        Guid vehicleTypeId,
        Guid driverId,
        string make,
        string model,
        string year,
        string color,
        string licensePlate)
        : base(id)
    {
        VehicleTypeId = vehicleTypeId;
        DriverId = driverId;
        Make = make;
        Model = model;
        Year = year;
        Color = color;
        LicensePlate = licensePlate;
        IsActive = true;
    }

    public Guid VehicleTypeId { get; private set; }
    public Guid DriverId { get; private set; }
    public string Make { get; private set; } = default!;
    public string Model { get; private set; } = default!;
    public string Year { get; private set; } = default!;
    public string Color { get; private set; } = default!;
    public string LicensePlate { get; private set; } = default!;
    public bool IsActive { get; private set; }

    public static Result<Vehicle> Create(
        Guid id,
        Guid vehicleTypeId,
        Guid driverId,
        string make,
        string model,
        string year,
        string color,
        string licensePlate)
    {
        if (vehicleTypeId == Guid.Empty)
        {
            return Error.Validation(LocalizationKeys.Vehicle.TypeIdRequired, "Vehicle type is required.");
        }

        if (string.IsNullOrWhiteSpace(make)) return Error.Validation(LocalizationKeys.Vehicle.MakeRequired, "Make is required.");
        if (string.IsNullOrWhiteSpace(model)) return Error.Validation(LocalizationKeys.Vehicle.ModelRequired, "Model is required.");
        if (string.IsNullOrWhiteSpace(licensePlate)) return Error.Validation(LocalizationKeys.Vehicle.LicensePlateRequired, "License plate is required.");

        return new Vehicle(id, vehicleTypeId, driverId, make, model, year, color, licensePlate);
    }

    public void UpdateDetails(string color, string licensePlate)
    {
        Color = color;
        LicensePlate = licensePlate;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
