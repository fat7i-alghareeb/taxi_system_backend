using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.CompleteStop;

public class CompleteStopCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<CompleteStopCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(CompleteStopCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var driverUserId))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Auth.Unauthorized, "Unauthorized user."));
        }

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == driverUserId, ct);
        if (driver is null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Driver.NotFound, "Driver profile not found."));
        }

        // Stops are an owned collection — include them so the aggregate has
        // them loaded before we mutate.
        var trip = await _context.Trips
            .Include(t => t.Stops)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, ct);

        if (trip is null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Trip.NotFound, "Trip not found."));
        }

        if (trip.DriverId != driver.Id)
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Trip.DriverMismatch, "This trip is not assigned to you."));
        }

        var transitionResult = trip.CompleteStop(request.Sequence);
        if (transitionResult.IsFailure)
        {
            return transitionResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }
}
