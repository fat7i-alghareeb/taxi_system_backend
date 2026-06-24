using MediatR;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Config.UpdateSupportContact;

public record UpdateSupportContactCommand(string? WhatsApp)
    : IRequest<Result<SupportContactDto>>;
