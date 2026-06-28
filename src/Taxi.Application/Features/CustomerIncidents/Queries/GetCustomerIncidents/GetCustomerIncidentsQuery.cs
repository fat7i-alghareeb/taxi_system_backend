using MediatR;
using Taxi.Application.Features.CustomerIncidents.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.CustomerIncidents.Queries.GetCustomerIncidents;

/// <summary>
/// Admin incident feed. All filters are optional — passing none returns everything
/// (most recent first). <paramref name="PassengerId"/> powers the "all logs for this
/// customer" view; <paramref name="Type"/> powers the type filter.
/// </summary>
public record GetCustomerIncidentsQuery(
    string? Type = null,
    Guid? PassengerId = null,
    string? Status = null,
    string? Severity = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<List<CustomerIncidentDto>>>;
