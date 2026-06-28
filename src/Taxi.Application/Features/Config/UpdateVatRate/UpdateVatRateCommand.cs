using MediatR;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.UpdateVatRate;

public record UpdateVatRateCommand(decimal Rate) : IRequest<Result<VatRateDto>>;
