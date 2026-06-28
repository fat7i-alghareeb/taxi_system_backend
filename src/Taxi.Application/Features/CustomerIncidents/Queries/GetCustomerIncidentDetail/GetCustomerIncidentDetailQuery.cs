using MediatR;
using Taxi.Application.Features.CustomerIncidents.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.CustomerIncidents.Queries.GetCustomerIncidentDetail;

public record GetCustomerIncidentDetailQuery(Guid IncidentId)
    : IRequest<Result<CustomerIncidentDetailDto>>;
