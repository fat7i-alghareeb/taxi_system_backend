using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetAllTrips;

public record GetAllTripsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Status = null,
    Guid? DriverId = null) : IRequest<Result<List<TripDto>>>;
