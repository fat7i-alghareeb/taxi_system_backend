using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.SubmitCompensationClaim;

public sealed class SubmitCompensationClaimCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<SubmitCompensationClaimCommand, Result<CompensationClaimDto>>
{
    public async Task<Result<CompensationClaimDto>> Handle(SubmitCompensationClaimCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id) || !Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (trip.PassengerId != passengerId)
        {
            return TripErrors.NotOwnedByPassenger;
        }

        var existingPendingClaim = await context.TripCompensationClaims
            .AnyAsync(c => c.TripId == trip.Id && c.Status == CompensationClaimStatus.Pending, ct);
        if (existingPendingClaim)
        {
            return TripErrors.CompensationClaimAlreadyReviewed;
        }

        var quote = await context.PricingQuotes.FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);
        var fare = quote?.FinalFare ?? 0;
        var currency = quote?.CurrencyCode ?? "EUR";

        // Policy: driver >20 min late (with proof) => 5% of the fare compensated.
        var claimResult = TripCompensationClaim.Create(
            Guid.NewGuid(),
            trip.Id,
            passengerId,
            request.Note,
            request.EvidenceUrls ?? [],
            Math.Round(fare * 0.05m, 2, MidpointRounding.AwayFromZero),
            currency);

        if (claimResult.IsError)
        {
            return claimResult.Errors;
        }

        context.TripCompensationClaims.Add(claimResult.Value);
        await context.SaveChangesAsync(ct);

        return claimResult.Value.ToDto();
    }
}
