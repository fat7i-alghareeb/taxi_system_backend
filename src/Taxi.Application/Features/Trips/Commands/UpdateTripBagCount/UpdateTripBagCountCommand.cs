using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.UpdateTripBagCount;

public record UpdateTripBagCountCommand(
    Guid TripId,
    int BagCount) : IRequest<Result<Success>>;
