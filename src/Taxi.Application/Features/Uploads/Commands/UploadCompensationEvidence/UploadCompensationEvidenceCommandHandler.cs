using MediatR;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Storage;
using Taxi.Application.Common.Uploads;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Uploads.Commands.UploadCompensationEvidence;

public class UploadCompensationEvidenceCommandHandler(IFileStorage fileStorage)
    : IRequestHandler<UploadCompensationEvidenceCommand, Result<IReadOnlyList<string>>>
{
    private readonly IFileStorage _fileStorage = fileStorage;

    public async Task<Result<IReadOnlyList<string>>> Handle(
        UploadCompensationEvidenceCommand request,
        CancellationToken ct)
    {
        var urls = new List<string>(request.Files.Count);
        foreach (var item in request.Files)
        {
            var validation = ImageUploadPolicy.Validate(item.Stream, item.FileName);
            if (validation.IsFailure)
            {
                return validation.Error;
            }

            var extension = Path.GetExtension(item.FileName);
            var url = await _fileStorage.SaveAsync(
                item.Stream,
                StoragePaths.CompensationEvidence(extension),
                ct);
            urls.Add(url);
        }

        return urls;
    }
}
