using Taxi.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.UpdateDriverLocation;

public class UpdateDriverLocationCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<UpdateDriverLocationCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(UpdateDriverLocationCommand request, CancellationToken ct)
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

        var updateResult = driver.UpdateLocation((decimal)request.Latitude, (decimal)request.Longitude);
        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }
}
