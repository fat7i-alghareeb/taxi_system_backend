using MediatR;
using Taxi.Application.Features.Uploads.Commands.UploadCompensationEvidence;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.UploadTripRecording;

// The file is passed as a stream + filename (UploadFileItem) so the Application layer
// stays free of ASP.NET dependencies. The controller maps IFormFile → UploadFileItem.
public record UploadTripRecordingCommand(
    Guid TripId,
    UploadFileItem File,
    int? DurationSeconds)
    : IRequest<Result<string>>;
