using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.AssignDriverToTrip;

public class AssignDriverToTripCommandHandler(IAppDbContext context)
    : IRequestHandler<AssignDriverToTripCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(AssignDriverToTripCommand request, CancellationToken ct)
    {
        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Trip.NotFound, "Trip not found."));
        }

        // Scheduled trips are locked until their start time arrives, even for admin
        // assignment. Clients enforce the same gate, but we re-check here so a stale
        // client or a direct API call cannot beat the schedule.
        if (trip.ScheduledAtUtc.HasValue && trip.ScheduledAtUtc.Value > DateTimeOffset.UtcNow)
        {
            return Result.Failure<Success>(Error.Validation(
                LocalizationKeys.Trip.ScheduledNotReady,
                "This trip is scheduled and cannot be assigned until its scheduled time."));
        }

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId, ct);
        if (driver == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Driver.NotFound, "Driver not found."));
        }

        if (driver.ApprovalStatus != DriverApprovalStatus.Approved)
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Driver.NotApproved, "Selected driver is not approved."));
        }

        if (!driver.IsActive)
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Driver.Inactive, "Selected driver is inactive."));
        }

        if (driver.VehicleTypeId is null)
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Driver.NoActiveVehicle, "Selected driver has no vehicle type assigned."));
        }

        if (driver.VehicleTypeId != trip.VehicleTypeId)
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Trip.VehicleTypeNotFound, "Selected driver does not operate the requested vehicle type."));
        }

        var assignResult = trip.AssignDriver(driver.Id);
        if (assignResult.IsFailure)
        {
            return assignResult.Error;
        }

        // Set driver status to OnTrip (or keep as is, but setting Status = OnTrip is standard)
        var statusResult = driver.SetStatus(DriverStatus.OnTrip);
        if (statusResult.IsFailure)
        {
            return statusResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }
}