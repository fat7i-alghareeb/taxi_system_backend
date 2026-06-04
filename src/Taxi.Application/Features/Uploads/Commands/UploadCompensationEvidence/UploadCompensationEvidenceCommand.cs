using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Uploads.Commands.UploadCompensationEvidence;

public record UploadCompensationEvidenceCommand(IReadOnlyList<UploadFileItem> Files)
    : IRequest<Result<IReadOnlyList<string>>>;

// File items are passed as a stream + filename rather than IFormFile so the Application
// layer stays free of ASP.NET dependencies. The controller maps IFormFile → UploadFileItem.
public record UploadFileItem(Stream Stream, string FileName);
