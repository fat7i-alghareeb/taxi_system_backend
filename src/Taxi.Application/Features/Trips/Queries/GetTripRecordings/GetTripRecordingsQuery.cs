using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetTripRecordings;

public record GetTripRecordingsQuery(Guid TripId) : IRequest<Result<List<TripRecordingDto>>>;

public record TripRecordingDto(
    Guid Id,
    Guid TripId,
    string FileUrl,
    string Type,
    int? DurationSeconds,
    DateTimeOffset RecordedAtUtc);
