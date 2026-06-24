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

    // The AppConfig invariant forbids empty values, so a blank field is modelled
    // as the absence of its key: upsert when set, delete the row when cleared.
    private async Task UpsertAsync(string key, string value, string description, CancellationToken ct)
    {
        var config = await _context.AppConfigs.FirstOrDefaultAsync(c => c.Key == key, ct);

        if (string.IsNullOrWhiteSpace(value))
        {
            if (config is not null)
            {
                _context.AppConfigs.Remove(config);
            }

            return;
        }

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
