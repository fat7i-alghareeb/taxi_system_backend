using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.ArriveTrip;

public record ArriveTripCommand(Guid TripId) : IRequest<Result<Success>>;
