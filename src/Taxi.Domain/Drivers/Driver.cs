using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Vehicles;

namespace Taxi.Domain.Drivers;

public sealed class Driver : AuditableEntity
{
    private Driver() { } // EF Core

    private Driver(Guid userId)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        Status = DriverStatus.Offline;
        AcceptanceRate = 1.0m;
        CompletionRate = 1.0m;
        TotalTripsCompleted = 0;
    }

    public Guid UserId { get; private set; }
    public Guid? ActiveVehicleId { get; private set; }
    public DriverStatus Status { get; private set; }
    public decimal? CurrentLat { get; private set; }
    public decimal? CurrentLng { get; private set; }
    public DateTimeOffset? LocationUpdatedAt { get; private set; }
    public decimal AcceptanceRate { get; private set; }
    public decimal CompletionRate { get; private set; }
    public int TotalTripsCompleted { get; private set; }

    public static Result<Driver> Create(Guid userId)
    {
        if (userId == Guid.Empty) return Error.Validation("Driver.UserIdRequired", "User ID is required.");
        return new Driver(userId);
    }

    public void UpdateLocation(decimal lat, decimal lng)
    {
        CurrentLat = lat;
        CurrentLng = lng;
        LocationUpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetStatus(DriverStatus status) => Status = status;
    public void SetActiveVehicle(Guid vehicleId) => ActiveVehicleId = vehicleId;
}

public enum DriverStatus
{
    Offline,
    Available,
    OnTrip
}
