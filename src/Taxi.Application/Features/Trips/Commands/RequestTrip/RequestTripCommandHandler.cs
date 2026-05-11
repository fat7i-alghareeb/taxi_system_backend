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
            new Domain.Trips.Coordinate(s.Latitude, s.Longitude),
            index,
            s.Label)).ToList();

        if (stopResults.Any(r => r.IsFailure))
        {
            return stopResults.First(r => r.IsFailure).Error;
        }

        var stops = stopResults.Select(r => r.Value).ToList();

        var referenceCode = $"TRP-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var tripResult = Trip.Request(
            Guid.NewGuid(),
            referenceCode,
            passengerId,
            quote,
            stops,
            request.ScheduledAt);

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

        var assignResult = trip.AssignDriver(adminDriver.UserId);

        if (assignResult.IsFailure)
        {
            return assignResult.Error;
        }

        quote.MarkAsUsed();
        _context.Trips.Add(trip);
        await _context.SaveChangesAsync(ct);

        var stopDtos = trip.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new TripStopDto(s.Coordinate.Latitude, s.Coordinate.Longitude))
            .ToList();

        return new TripDto(
            trip.Id,
            trip.ReferenceCode,
            trip.PassengerId,
            trip.DriverId,
            trip.VehicleTypeId,
            trip.Status.ToString(),
            quote.FinalFare,
            quote.CurrencyCode,
            trip.CreatedAtUtc,
            trip.ScheduledAtUtc,
            stopDtos);
    }
}

