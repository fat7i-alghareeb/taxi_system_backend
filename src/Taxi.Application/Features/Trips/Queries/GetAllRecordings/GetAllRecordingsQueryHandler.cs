using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetAllRecordings;

public class GetAllRecordingsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAllRecordingsQuery, Result<List<AdminRecordingDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<AdminRecordingDto>>> Handle(GetAllRecordingsQuery request, CancellationToken ct)
    {
        var query =
            from r in _context.TripRecordings.AsNoTracking()
            where r.DeletedAtUtc == null
            join t in _context.Trips.AsNoTracking() on r.TripId equals t.Id
            select new { Recording = r, Trip = t };

        if (request.PassengerId.HasValue)
        {
            query = query.Where(x => x.Recording.PassengerId == request.PassengerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.Trip.ReferenceCode.ToLower().Contains(search));
        }

        var recordings = await query
            .OrderByDescending(x => x.Recording.RecordedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AdminRecordingDto(
                x.Recording.Id,
                x.Recording.TripId,
                x.Trip.ReferenceCode,
                x.Recording.PassengerId,
                _context.DomainUsers
                    .Where(u => u.Id == x.Recording.PassengerId)
                    .Select(u => u.Name)
                    .FirstOrDefault(),
                x.Recording.FileUrl,
                x.Recording.Type.ToString(),
                x.Recording.DurationSeconds,
                x.Recording.RecordedAtUtc))
            .ToListAsync(ct);

        return recordings;
    }
}
