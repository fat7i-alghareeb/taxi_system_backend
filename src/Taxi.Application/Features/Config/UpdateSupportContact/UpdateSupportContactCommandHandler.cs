using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;

namespace Taxi.Application.Features.Config.UpdateSupportContact;

public class UpdateSupportContactCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateSupportContactCommand, Result<SupportContactDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<SupportContactDto>> Handle(UpdateSupportContactCommand request, CancellationToken ct)
    {
        var whatsApp = request.WhatsApp?.Trim() ?? string.Empty;

        await UpsertAsync(
            AppConfigKeys.SupportWhatsApp,
            whatsApp,
            "Support WhatsApp number for the in-trip \"Report problem\" action.",
            ct);

        await _context.SaveChangesAsync(ct);
        return new SupportContactDto(whatsApp);
    }

    private async Task UpsertAsync(string key, string value, string description, CancellationToken ct)
    {
        var config = await _context.AppConfigs.FirstOrDefaultAsync(c => c.Key == key, ct);

        if (config is null)
        {
            var createResult = AppConfig.Create(key, value, description);
            if (createResult.IsSuccess)
            {
                _context.AppConfigs.Add(createResult.Value);
            }
        }
        else
        {
            config.UpdateValue(value);
        }
    }
}
