using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.CompleteTrip;

public record CompleteTripCommand(Guid TripId) : IRequest<Result<Success>>;
