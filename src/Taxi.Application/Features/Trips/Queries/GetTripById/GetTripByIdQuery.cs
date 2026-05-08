using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetTripById;

public record GetTripByIdQuery(Guid Id) : IRequest<Result<TripDto>>;

