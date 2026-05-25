using MediatR;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;

namespace Taxi.Application.Features.Drivers.Queries.GetDriverDocuments;

public record DriverDocumentDto(
    Guid Id,
    string Type,
    string FileUrl,
    string Status,
    string? ReviewNotes,
    DateTimeOffset? ReviewedAtUtc);

public record GetDriverDocumentsQuery(Guid DriverId) : IRequest<Result<List<DriverDocumentDto>>>;
