using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.ReviewDriverDocument;

public record ReviewDriverDocumentCommand(
    Guid DriverId,
    Guid DocumentId,
    bool Approved,
    string? Notes = null) : IRequest<Result<Success>>;
