using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;

namespace Taxi.Application.Features.Drivers.Commands.ApproveDriver;

public class ApproveDriverCommandHandler(IAppDbContext context)
    : IRequestHandler<ApproveDriverCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(ApproveDriverCommand request, CancellationToken ct)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId, ct);
        if (driver == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Driver.NotFound, "Driver profile not found."));
        }

        // Verify if all document types are approved
        var unapprovedDocs = await _context.DriverDocuments
            .AnyAsync(dd => dd.DriverId == driver.Id && dd.Status != DocumentStatus.Approved && dd.DeletedAtUtc == null, ct);

        if (unapprovedDocs)
        {
            return Result.Failure<Success>(Error.Validation(LocalizationKeys.Driver.DocumentsNotApproved, "Cannot approve driver: Some documents are not approved."));
        }

        var approveResult = driver.Approve();
        if (approveResult.IsFailure)
        {
            return approveResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }
}
