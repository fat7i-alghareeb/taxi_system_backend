using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.RateTrip;

public record RateTripCommand(
    Guid TripId,
    int Stars,
    string? Comment) : IRequest<Result<Success>>;
