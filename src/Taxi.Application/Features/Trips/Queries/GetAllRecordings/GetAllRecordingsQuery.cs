using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Queries.GetAllRecordings;

public record GetAllRecordingsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? PassengerId = null,
    string? Search = null) : IRequest<Result<List<AdminRecordingDto>>>;

public record AdminRecordingDto(
    Guid Id,
    Guid TripId,
    string TripReferenceCode,
    Guid PassengerId,
    string? PassengerName,
    string FileUrl,
    string Type,
    int? DurationSeconds,
    DateTimeOffset RecordedAtUtc);
