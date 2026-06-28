using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Storage;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;

namespace Taxi.Application.Features.Drivers.Commands.UploadDriverDocument;

public class UploadDriverDocumentCommandHandler(IAppDbContext context, IFileStorage fileStorage)
    : IRequestHandler<UploadDriverDocumentCommand, Result<string>>
{
    private readonly IAppDbContext _context = context;
    private readonly IFileStorage _fileStorage = fileStorage;

    public async Task<Result<string>> Handle(UploadDriverDocumentCommand request, CancellationToken ct)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId, ct);
        if (driver == null)
        {
            return DriverErrors.NotFound;
        }

        // Soft-delete an existing document of the same type so we never store duplicates.
        var existingDoc = await _context.DriverDocuments
            .FirstOrDefaultAsync(dd => dd.DriverId == driver.Id && dd.Type == request.Type, ct);
        if (existingDoc != null)
        {
            var softDeleteResult = existingDoc.SoftDelete();
            if (softDeleteResult.IsFailure)
            {
                return softDeleteResult.Error;
            }
        }

        var extension = Path.GetExtension(request.FileName);
        var fileUrl = await _fileStorage.SaveAsync(
            request.FileStream,
            StoragePaths.DriverDocument(driver.Id, request.Type, extension),
            ct);

        var documentResult = DriverDocument.Create(Guid.NewGuid(), driver.Id, request.Type, fileUrl);
        if (documentResult.IsFailure)
        {
            return documentResult.Error;
        }

        _context.DriverDocuments.Add(documentResult.Value);

        // Auto-submit for review once every required document type has been uploaded.
        var existingDocs = await _context.DriverDocuments
            .Where(dd => dd.DriverId == driver.Id && dd.DeletedAtUtc == null)
            .Select(dd => dd.Type)
            .ToListAsync(ct);

        var allUploaded = Enum.GetValues<DocumentType>()
            .All(t => t == request.Type || existingDocs.Contains(t));

        if (allUploaded && driver.ApprovalStatus == DriverApprovalStatus.PendingDocuments)
        {
            driver.SubmitForReview();
        }

        await _context.SaveChangesAsync(ct);

        return fileUrl;
    }
}
