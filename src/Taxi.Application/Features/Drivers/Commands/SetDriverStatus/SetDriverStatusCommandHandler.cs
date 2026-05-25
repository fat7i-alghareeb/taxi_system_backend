using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;

namespace Taxi.Application.Features.Drivers.Commands.SetDriverStatus;

public class SetDriverStatusCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<SetDriverStatusCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(SetDriverStatusCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var driverUserId))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Auth.Unauthorized, "Unauthorized user."));
        }

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == driverUserId, ct);
        if (driver == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Driver.NotFound, "Driver profile not found."));
        }

        if (request.Status == DriverStatus.Online)
        {
            if (driver.VehicleTypeId == null)
            {
                return Result.Failure<Success>(Error.Validation(LocalizationKeys.Driver.NoActiveVehicle, "A vehicle type must be assigned before going online."));
            }
        }

        var statusResult = driver.SetStatus(request.Status);
        if (statusResult.IsFailure)
        {
            return statusResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }
}