using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetCompensationClaims;

public sealed class GetCompensationClaimsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetCompensationClaimsQuery, Result<List<CompensationClaimDto>>>
{
    public async Task<Result<List<CompensationClaimDto>>> Handle(GetCompensationClaimsQuery request, CancellationToken ct)
    {
        var query = context.TripCompensationClaims.AsQueryable();
        if (Enum.TryParse<CompensationClaimStatus>(request.Status, ignoreCase: true, out var status))
        {
            query = query.Where(c => c.Status == status);
        }

        var claims = await query.OrderByDescending(c => c.CreatedAtUtc).Take(100).ToListAsync(ct);
        return claims.Select(c => c.ToDto()).ToList();
    }
}
