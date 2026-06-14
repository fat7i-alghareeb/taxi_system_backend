using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Responses.Config;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Configuration;

namespace Taxi.Application.Features.Config.GetCompanyContact;

public class GetCompanyContactQueryHandler(IAppDbContext context)
    : IRequestHandler<GetCompanyContactQuery, Result<CompanyContactDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<CompanyContactDto>> Handle(GetCompanyContactQuery request, CancellationToken ct)
    {
        var keys = new[] { AppConfigKeys.CompanyEmail, AppConfigKeys.CompanyPhone, AppConfigKeys.CompanyWebsite };

        var values = await _context.AppConfigs
            .AsNoTracking()
            .Where(c => keys.Contains(c.Key))
            .ToDictionaryAsync(c => c.Key, c => c.Value, ct);

        return new CompanyContactDto(
            Email: values.GetValueOrDefault(AppConfigKeys.CompanyEmail) ?? string.Empty,
            Phone: values.GetValueOrDefault(AppConfigKeys.CompanyPhone) ?? string.Empty,
            Website: values.GetValueOrDefault(AppConfigKeys.CompanyWebsite) ?? string.Empty);
    }
}
