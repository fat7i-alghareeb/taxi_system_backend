using System.Security.Cryptography;
using System.Text;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.CustomerIncidents;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.RateTrip;

public class RateTripCommandHandler(IAppDbContext context, IUser currentUser, ICustomerIncidentRecorder incidentRecorder)
    : IRequestHandler<RateTripCommand, Result<Success>>
{
    // A trip rated this low or lower raises a customer incident for admin follow-up.
    private const int LowRatingThreshold = 3;

    public async Task<Result<Success>> Handle(RateTripCommand request, CancellationToken ct)
    {
        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        // Only the trip's own passenger may rate it (admins excepted).
        if (!currentUser.IsAdmin)
        {
            if (!Guid.TryParse(currentUser.Id, out var passengerId))
            {
                return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
            }

            if (trip.PassengerId != passengerId)
            {
                return TripErrors.NotOwnedByPassenger;
            }
        }

        var rateResult = trip.Rate(request.Stars, request.Comment);
        if (rateResult.IsFailure)
        {
            return rateResult.Error;
        }

        await context.SaveChangesAsync(ct);

        if (request.Stars <= LowRatingThreshold)
        {
            var reason = string.IsNullOrWhiteSpace(request.Comment)
                ? $"Rated {request.Stars}★"
                : $"Rated {request.Stars}★: {request.Comment}";

            await incidentRecorder.RecordAsync(
                CustomerIncidentType.LowRating,
                CustomerIncidentSeverity.Warning,
                trip.PassengerId,
                title: "Low trip rating",
                // No domain event backs a rating, so derive a stable id per trip to
                // keep the incident idempotent if the trip is re-rated.
                sourceEventId: DeterministicEventId($"LowRating:{trip.Id}"),
                tripId: trip.Id,
                reason: reason,
                ct: ct);
        }

        return Result.Success;
    }

    private static Guid DeterministicEventId(string seed)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(seed));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }
}
