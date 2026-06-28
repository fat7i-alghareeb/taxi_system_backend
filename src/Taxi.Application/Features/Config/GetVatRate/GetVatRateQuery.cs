using MediatR;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.GetVatRate;

public record GetVatRateQuery : IRequest<Result<VatRateDto>>;
