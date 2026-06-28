using MediatR;
using Taxi.Application.Features.CustomerIncidents.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.CustomerIncidents.Commands.ChangeIncidentStatus;

public record ChangeIncidentStatusCommand(Guid IncidentId, string Status, string? Note)
    : IRequest<Result<CustomerIncidentDto>>;
