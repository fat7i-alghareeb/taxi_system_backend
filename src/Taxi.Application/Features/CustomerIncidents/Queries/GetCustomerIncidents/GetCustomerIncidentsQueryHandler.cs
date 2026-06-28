using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.CustomerIncidents.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.CustomerIncidents;

namespace Taxi.Application.Features.CustomerIncidents.Queries.GetCustomerIncidents;

public sealed class GetCustomerIncidentsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetCustomerIncidentsQuery, Result<List<CustomerIncidentDto>>>
{
    public async Task<Result<List<CustomerIncidentDto>>> Handle(GetCustomerIncidentsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var query = context.CustomerIncidents.AsNoTracking();

        if (request.PassengerId is { } passengerId && passengerId != Guid.Empty)
        {
            query = query.Where(i => i.PassengerId == passengerId);
        }

        if (Enum.TryParse<CustomerIncidentType>(request.Type, ignoreCase: true, out var type))
        {
            query = query.Where(i => i.Type == type);
        }

        if (Enum.TryParse<CustomerIncidentStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (Enum.TryParse<CustomerIncidentSeverity>(request.Severity, ignoreCase: true, out var severity))
        {
            query = query.Where(i => i.Severity == severity);
        }

        var incidents = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        if (incidents.Count == 0)
        {
            return new List<CustomerIncidentDto>();
        }

        var passengerIds = incidents.Select(i => i.PassengerId).Distinct().ToList();
        var passengerNames = await context.DomainUsers
            .AsNoTracking()
            .Where(u => passengerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        var tripIds = incidents.Where(i => i.TripId.HasValue).Select(i => i.TripId!.Value).Distinct().ToList();
        var tripRefs = tripIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await context.Trips
                .AsNoTracking()
                .Where(t => tripIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.ReferenceCode, ct);

        return incidents
            .Select(i => i.ToDto(
                passengerNames.GetValueOrDefault(i.PassengerId),
                i.TripId is { } tid ? tripRefs.GetValueOrDefault(tid) : null))
            .ToList();
    }
}
