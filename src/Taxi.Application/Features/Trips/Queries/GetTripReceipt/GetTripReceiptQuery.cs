using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetTripReceipt;

public record GetTripReceiptQuery(Guid TripId) : IRequest<Result<TripReceiptDto>>;
