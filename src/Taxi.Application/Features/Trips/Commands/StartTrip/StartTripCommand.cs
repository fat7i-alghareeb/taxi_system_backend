using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.StartTrip;

public record StartTripCommand(Guid TripId) : IRequest<Result<Success>>;
