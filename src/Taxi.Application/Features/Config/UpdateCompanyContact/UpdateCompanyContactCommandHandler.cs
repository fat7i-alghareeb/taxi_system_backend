using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;

namespace Taxi.Application.Features.Config.UpdateCompanyContact;

public class UpdateCompanyContactCommandHandler(IAppDbContext context, HybridCache cache)
    : IRequestHandler<UpdateCompanyContactCommand, Result<CompanyContactDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly HybridCache _cache = cache;

    public async Task<Result<CompanyContactDto>> Handle(UpdateCompanyContactCommand request, CancellationToken ct)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        var phone = request.Phone?.Trim() ?? string.Empty;
        var website = request.Website?.Trim() ?? string.Empty;

        await UpsertAsync(AppConfigKeys.CompanyEmail, email, "Company email shown on invoices.", ct);
        await UpsertAsync(AppConfigKeys.CompanyPhone, phone, "Company phone shown on invoices.", ct);
        await UpsertAsync(AppConfigKeys.CompanyWebsite, website, "Company website shown on invoices.", ct);

        await _context.SaveChangesAsync(ct);

        // Shared with the combined bootstrap payload, which embeds this value.
        await _cache.RemoveByTagAsync("app-config", ct);
        return new CompanyContactDto(email, phone, website);
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
