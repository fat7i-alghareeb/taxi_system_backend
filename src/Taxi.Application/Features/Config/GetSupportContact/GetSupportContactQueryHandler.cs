using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;

namespace Taxi.Application.Features.Config.GetSupportContact;

public class GetSupportContactQueryHandler(IAppDbContext context)
    : IRequestHandler<GetSupportContactQuery, Result<SupportContactDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<SupportContactDto>> Handle(GetSupportContactQuery request, CancellationToken ct)
    {
        var value = await _context.AppConfigs
            .AsNoTracking()
            .Where(c => c.Key == AppConfigKeys.SupportWhatsApp)
            .Select(c => c.Value)
            .FirstOrDefaultAsync(ct);

        return new SupportContactDto(WhatsApp: value ?? string.Empty);
    }
}
