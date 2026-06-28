using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetTripRecordings;

public class GetTripRecordingsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetTripRecordingsQuery, Result<List<TripRecordingDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<TripRecordingDto>>> Handle(GetTripRecordingsQuery request, CancellationToken ct)
    {
        var recordings = await _context.TripRecordings
            .AsNoTracking()
            .Where(r => r.TripId == request.TripId && r.DeletedAtUtc == null)
            .OrderBy(r => r.RecordedAtUtc)
            .Select(r => new TripRecordingDto(
                r.Id,
                r.TripId,
                r.FileUrl,
                r.Type.ToString(),
                r.DurationSeconds,
                r.RecordedAtUtc))
            .ToListAsync(ct);

        return recordings;
    }
}
