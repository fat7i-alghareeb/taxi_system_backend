using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetTripById;

public class GetTripByIdQueryHandler(
    IAppDbContext context) : IRequestHandler<GetTripByIdQuery, Result<TripDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<TripDto>> Handle(GetTripByIdQuery request, CancellationToken ct)
    {
        var trip = await _context.Trips
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct);

        if (trip is null)
        {
            return Error.NotFound("Trip.NotFound", "Trip not found.");
        }

        var quote = await _context.PricingQuotes
            .FirstOrDefaultAsync(q => q.Id == trip.QuoteId, ct);

        return new TripDto(
            trip.Id,
            trip.ReferenceCode,
            trip.PassengerId,
            trip.DriverId,
            trip.VehicleTypeId,
            trip.Status.ToString(),
            quote?.FinalFare ?? 0,
            quote?.CurrencyCode ?? "USD",
            trip.CreatedAtUtc);
    }
}
