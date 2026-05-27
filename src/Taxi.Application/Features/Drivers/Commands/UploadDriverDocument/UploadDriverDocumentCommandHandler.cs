using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;

namespace Taxi.Application.Features.Drivers.Commands.UploadDriverDocument;

public class UploadDriverDocumentCommandHandler(IAppDbContext context)
    : IRequestHandler<UploadDriverDocumentCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(UploadDriverDocumentCommand request, CancellationToken ct)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId, ct);
        if (driver == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Driver.NotFound, "Driver not found."));
        }

        // Delete existing document of same type if it exists to avoid duplicates
        var existingDoc = await _context.DriverDocuments
            .FirstOrDefaultAsync(dd => dd.DriverId == driver.Id && dd.Type == request.Type, ct);
        if (existingDoc != null)
        {
            existingDoc.SoftDelete();
        }

        var documentResult = DriverDocument.Create(Guid.NewGuid(), driver.Id, request.Type, request.FileUrl);
        if (documentResult.IsFailure)
        {
            return documentResult.Error;
        }

        _context.DriverDocuments.Add(documentResult.Value);

        // Check if all document types have been uploaded to submit for review
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

        return Result.Success;
    }
}
