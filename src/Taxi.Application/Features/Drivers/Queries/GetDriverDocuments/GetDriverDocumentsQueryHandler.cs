using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Queries.GetDriverDocuments;

public class GetDriverDocumentsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetDriverDocumentsQuery, Result<List<DriverDocumentDto>>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<List<DriverDocumentDto>>> Handle(GetDriverDocumentsQuery request, CancellationToken ct)
    {
        var documents = await _context.DriverDocuments
            .Where(dd => dd.DriverId == request.DriverId && dd.DeletedAtUtc == null)
            .Select(dd => new DriverDocumentDto(
                dd.Id,
                dd.Type.ToString(),
                dd.FileUrl,
                dd.Status.ToString(),
                dd.ReviewNotes,
                dd.ReviewedAtUtc))
            .ToListAsync(ct);

        return documents;
    }
}
