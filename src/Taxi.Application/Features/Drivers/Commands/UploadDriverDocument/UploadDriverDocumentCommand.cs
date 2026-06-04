using MediatR;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;

namespace Taxi.Application.Features.Drivers.Commands.UploadDriverDocument;

// Handler owns the file persistence via IFileStorage so the controller stays a thin
// HTTP-binding wrapper. Returns the persisted file URL so the client can show/cache it.
public record UploadDriverDocumentCommand(
    Guid DriverId,
    DocumentType Type,
    Stream FileStream,
    string FileName) : IRequest<Result<string>>;
