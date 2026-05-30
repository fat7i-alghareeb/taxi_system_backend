using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.CompleteTrip;

public class CompleteTripCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<CompleteTripCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(CompleteTripCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var driverUserId))
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Auth.Unauthorized, "Unauthorized user."));
        }

        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Trip.NotFound, "Trip not found."));
        }

        // Admins can act on any trip; drivers only on trips assigned to them.
        if (!currentUser.IsAdmin)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == driverUserId, ct);
            if (driver == null)
            {
                return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Driver.NotFound, "Driver profile not found."));
            }

            if (trip.DriverId != driver.Id)
            {
                return Result.Failure<Success>(Error.Validation(LocalizationKeys.Trip.DriverMismatch, "This trip is not assigned to you."));
            }
        }

        var transitionResult = trip.Complete();
        if (transitionResult.IsFailure)
        {
            return transitionResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }
}
