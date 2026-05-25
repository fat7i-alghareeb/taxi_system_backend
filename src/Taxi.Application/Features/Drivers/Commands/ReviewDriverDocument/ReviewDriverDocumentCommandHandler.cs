using Taxi.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;

namespace Taxi.Application.Features.Drivers.Commands.ReviewDriverDocument;

public class ReviewDriverDocumentCommandHandler(IAppDbContext context)
    : IRequestHandler<ReviewDriverDocumentCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(ReviewDriverDocumentCommand request, CancellationToken ct)
    {
        var document = await _context.DriverDocuments
            .FirstOrDefaultAsync(dd => dd.Id == request.DocumentId && dd.DriverId == request.DriverId, ct);

        if (document == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.DriverDocument.NotFound, "Driver document not found."));
        }

        if (request.Approved)
        {
            var approveResult = document.Approve(request.Notes);
            if (approveResult.IsFailure)
            {
                return approveResult.Error;
            }
        }
        else
        {
            var rejectResult = document.Reject(request.Notes ?? "Document rejected by Administrator.");
            if (rejectResult.IsFailure)
            {
                return rejectResult.Error;
            }
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }
}
