using MediatR;

using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Trips.Dtos;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Queries.GetTripById;

public class GetTripByIdQueryHandler(
    IAppDbContext context,
    TimeProvider timeProvider) : IRequestHandler<GetTripByIdQuery, Result<TripDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<TripDto>> Handle(GetTripByIdQuery request, CancellationToken ct)
    {
        var trip = await _context.Trips
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct);

        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        return await TripDtoBuilder.BuildAsync(_context, trip, timeProvider.GetUtcNow(), ct);
    }
}
