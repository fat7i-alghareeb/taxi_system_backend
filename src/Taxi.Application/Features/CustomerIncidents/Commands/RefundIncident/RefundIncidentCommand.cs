using MediatR;
using Taxi.Application.Features.CustomerIncidents.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.CustomerIncidents.Commands.RefundIncident;

/// <summary>
/// Issues a Stripe refund against the incident's trip fare. A null
/// <paramref name="Amount"/> refunds the full captured amount.
/// </summary>
public record RefundIncidentCommand(Guid IncidentId, decimal? Amount)
    : IRequest<Result<CustomerIncidentDto>>;
