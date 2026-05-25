using Taxi.Contracts.Common;
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

public enum DriverApprovalStatus
{
    PendingDocuments,
    UnderReview,
    Approved,
    Suspended
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
    public Guid? VehicleTypeId { get; private set; }
    public DriverStatus Status { get; private set; }
    public DriverApprovalStatus ApprovalStatus { get; private set; } = DriverApprovalStatus.PendingDocuments;
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
        if (status == DriverStatus.Online && ApprovalStatus != DriverApprovalStatus.Approved)
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Driver.NotApproved, "Driver must be approved before going online."));
        }

        Status = status;
        return Result.Success;
    }

    public Result<Success> SubmitForReview()
    {
        if (ApprovalStatus != DriverApprovalStatus.PendingDocuments)
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Driver.InvalidApprovalTransition, $"Cannot submit for review from status {ApprovalStatus}."));
        }

        ApprovalStatus = DriverApprovalStatus.UnderReview;
        return Result.Success;
    }

    public Result<Success> Approve()
    {
        ApprovalStatus = DriverApprovalStatus.Approved;
        return Result.Success;
    }

    public Result<Success> Suspend()
    {
        ApprovalStatus = DriverApprovalStatus.Suspended;
        Status = DriverStatus.Offline; // Force offline when suspended
        return Result.Success;
    }

    public Result<Success> SetVehicleType(Guid vehicleTypeId)
    {
        VehicleTypeId = vehicleTypeId;
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