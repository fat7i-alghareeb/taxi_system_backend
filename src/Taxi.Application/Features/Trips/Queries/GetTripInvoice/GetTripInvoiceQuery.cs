using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetTripInvoice;

public record GetTripInvoiceQuery(Guid TripId) : IRequest<Result<TripInvoiceDto>>;
