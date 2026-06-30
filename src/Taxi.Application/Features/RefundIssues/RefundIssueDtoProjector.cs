using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.RefundIssues.Dtos;
using Taxi.Domain.RefundIssues;

namespace Taxi.Application.Features.RefundIssues;

internal static class RefundIssueDtoProjector
{
    public static async Task<List<RefundIssueDto>> ToDtosAsync(
        IAppDbContext context,
        IReadOnlyCollection<RefundIssue> issues,
        CancellationToken ct)
    {
        if (issues.Count == 0)
        {
            return [];
        }

        var passengerIds = issues.Select(issue => issue.PassengerId).Distinct().ToList();
        var tripIds = issues.Select(issue => issue.TripId).Distinct().ToList();

        var passengerNames = await context.DomainUsers
            .AsNoTracking()
            .Where(user => passengerIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.Name, ct);

        var tripRefs = await context.Trips
            .AsNoTracking()
            .Where(trip => tripIds.Contains(trip.Id))
            .ToDictionaryAsync(trip => trip.Id, trip => trip.ReferenceCode, ct);

        return issues
            .Select(issue => issue.ToDto(
                passengerNames.GetValueOrDefault(issue.PassengerId),
                tripRefs.GetValueOrDefault(issue.TripId)))
            .ToList();
    }
}
