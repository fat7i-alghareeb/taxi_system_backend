using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Common;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.PreviewTripEdit;

public class PreviewTripEditCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    ITripRequoteService requoteService,
    ILanguageContext languageContext)
    : IRequestHandler<PreviewTripEditCommand, Result<TripEditPreviewDto>>
{
    public async Task<Result<TripEditPreviewDto>> Handle(PreviewTripEditCommand request, CancellationToken ct)
    {
        if (request.Stops is null && request.PassengerCount is null)
        {
            return TripErrors.InvalidStops;
        }

        var trip = await context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        if (!currentUser.IsAdmin)
        {
            if (!Guid.TryParse(currentUser.Id, out var passengerId))
            {
                return TripErrors.PassengerNotFound;
            }

            if (trip.PassengerId != passengerId)
            {
                return TripErrors.NotOwnedByPassenger;
            }
        }

        if (TripEditPolicy.Validate(
                trip,
                editsStops: request.Stops is { Count: > 0 },
                editsPartySize: request.PassengerCount.HasValue) is { } policyError)
        {
            return policyError;
        }

        var requote = await requoteService.RequoteAsync(trip, request.Stops, request.PassengerCount, ct);
        if (requote.IsFailure)
        {
            return requote.Error;
        }

        var r = requote.Value;

        var lang = languageContext.Language;
        string? newVehicleTypeName = null;
        if (r.NewVehicleTypeId.HasValue)
        {
            newVehicleTypeName = await context.VehicleTypes
                .Where(v => v.Id == r.NewVehicleTypeId.Value)
                .Select(v => lang == Languages.Ar ? v.Name.Ar : (lang == Languages.Nl ? v.Name.Nl : v.Name.En))
                .FirstOrDefaultAsync(ct);
        }

        var direction = r.Delta > 0 ? "charge" : r.Delta < 0 ? "refund" : "none";

        // Persist the re-priced quote UNUSED and hand its id back as the preview token. Apply
        // reuses this exact quote instead of routing again, so the delta the customer confirmed
        // is the delta that gets charged. An abandoned preview just leaves an unused quote
        // behind — the same lifecycle as the quotes produced when booking.
        context.PricingQuotes.Add(r.NewQuote);
        await context.SaveChangesAsync(ct);

        return new TripEditPreviewDto(
            r.OldFinalFare,
            r.NewFinalFare,
            r.Delta,
            r.CurrencyCode,
            r.NewVehicleTypeId,
            newVehicleTypeName,
            request.PassengerCount ?? trip.PassengerCount,
            direction,
            r.NewQuote.Id);
    }
}
