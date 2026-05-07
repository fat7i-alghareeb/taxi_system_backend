using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Trips.Commands.RequestTrip;

public class RequestTripCommandHandler(
    IAppDbContext context,
    IUser currentUser) : IRequestHandler<RequestTripCommand, Result<TripDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<TripDto>> Handle(RequestTripCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var passengerId))
        {
            return TripErrors.PassengerNotFound;
        }

        var passengerExists = await _context.DomainUsers
            .AnyAsync(u => u.Id == passengerId && u.Role == UserRole.Passenger, ct);

        if (!passengerExists)
        {
            return TripErrors.PassengerNotFound;
        }

        var quote = await _context.PricingQuotes
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId && q.PassengerId == passengerId, ct);

        if (quote is null)
        {
            return TripErrors.QuoteNotFound;
        }

        if (quote.IsExpired())
        {
            return TripErrors.QuoteExpired;
        }

        if (quote.Used)
        {
            return TripErrors.QuoteAlreadyUsed;
        }

        var stopResults = request.Stops.Select((s, index) => TripStop.Create(
            new Coordinate(s.Latitude, s.Longitude),
            index)).ToList();

        if (stopResults.Any(r => r.IsFailure))
        {
            return stopResults.First(r => r.IsFailure).Error;
        }

        var stops = stopResults.Select(r => r.Value).ToList();

        var tripResult = Trip.Request(
            Guid.NewGuid(),
            passengerId,
            quote,
            stops);

        if (tripResult.IsFailure)
        {
            return tripResult.Error;
        }

        var trip = tripResult.Value;

        var adminDriver = await _context.Drivers
            .FirstOrDefaultAsync(d => d.IsActive, ct);

        if (adminDriver is null)
        {
            return TripErrors.DriverNotFound;
        }

        var assignResult = trip.AssignDriver(adminDriver.Id);

        if (assignResult.IsFailure)
        {
            return assignResult.Error;
        }

        quote.MarkAsUsed();
        _context.Trips.Add(trip);
        await _context.SaveChangesAsync(ct);

        return new TripDto(
            trip.Id,
            trip.ReferenceCode,
            trip.PassengerId,
            trip.DriverId,
            trip.VehicleTypeId,
            trip.Status.ToString(),
            quote.FinalFare,
            quote.CurrencyCode,
            trip.CreatedAtUtc);
    }
}
