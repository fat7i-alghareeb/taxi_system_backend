using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.CustomerIncidents.Commands.ContactPassenger;

/// <summary>Sends an admin-written push message to the incident's passenger.</summary>
public record ContactPassengerCommand(Guid IncidentId, string Title, string Body)
    : IRequest<Result<Success>>;
