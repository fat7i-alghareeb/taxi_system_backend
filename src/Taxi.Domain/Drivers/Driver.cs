using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Drivers;

public enum DriverStatus
{
    Offline,
    Online,
    OnTrip,
    Blocked
}

public sealed class Driver : AuditableEntity
{
    private Driver() { } // EF Core

    private Driver(Guid id, Guid userId, string licenseNumber)
        : base(id)
    {
        UserId = userId;
        LicenseNumber = licenseNumber;
        Status = DriverStatus.Offline;
        AcceptanceRate = 1.0m;
        CompletionRate = 1.0m;
        TotalTripsCompleted = 0;
        IsActive = true;
    }

    public Guid UserId { get; private set; }
    public string LicenseNumber { get; private set; } = default!;
    public Guid? ActiveVehicleId { get; private set; }
    public DriverStatus Status { get; private set; }
    public decimal? CurrentLat { get; private set; }
    public decimal? CurrentLng { get; private set; }
    public DateTimeOffset? LocationUpdatedAt { get; private set; }
    public decimal AcceptanceRate { get; private set; }
    public decimal CompletionRate { get; private set; }
    public int TotalTripsCompleted { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Result<Driver> Create(Guid id, Guid userId, string licenseNumber)
    {
        if (userId == Guid.Empty)
        {
            return DriverErrors.UserIdRequired;
        }

        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            return DriverErrors.LicenseRequired;
        }

        return new Driver(id, userId, licenseNumber);
    }

    public Result<Success> UpdateLocation(decimal lat, decimal lng)
    {
        CurrentLat = lat;
        CurrentLng = lng;
        LocationUpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success;
    }

    public Result<Success> UpdateDetails(string licenseNumber)
    {
        LicenseNumber = licenseNumber;
        return Result.Success;
    }

    public Result<Success> SetStatus(DriverStatus status)
    {
        Status = status;
        return Result.Success;
    }

    public Result<Success> SetActiveVehicle(Guid vehicleId)
    {
        ActiveVehicleId = vehicleId;
        return Result.Success;
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
        DeletedAtUtc = DateTimeOffset.UtcNow;
        return Result.Success;
    }
}

