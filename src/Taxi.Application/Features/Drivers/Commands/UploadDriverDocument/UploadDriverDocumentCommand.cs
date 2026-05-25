using MediatR;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;

namespace Taxi.Application.Features.Drivers.Commands.UploadDriverDocument;

public record UploadDriverDocumentCommand(Guid DriverId, DocumentType Type, string FileUrl) : IRequest<Result<Success>>;
