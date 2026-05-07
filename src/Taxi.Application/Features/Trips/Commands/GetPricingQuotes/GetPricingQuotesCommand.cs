using MediatR;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.GetPricingQuotes;

public record GetPricingQuotesCommand(
    List<CoordinateDto> Stops) : IRequest<Result<List<PricingQuoteDto>>>;
