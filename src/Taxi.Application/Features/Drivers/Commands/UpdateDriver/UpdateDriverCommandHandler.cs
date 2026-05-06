using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.UpdateDriver;

public class UpdateDriverCommandHandler(
    IAppDbContext context,
    ILanguageContext languageContext) : IRequestHandler<UpdateDriverCommand, Result<DriverDto>>
{
    private readonly IAppDbContext _context = context;
    private readonly ILanguageContext _languageContext = languageContext;

    public async Task<Result<DriverDto>> Handle(UpdateDriverCommand request, CancellationToken ct)
    {
        var driver = await _context.Drivers
            .FirstOrDefaultAsync(d => d.Id == request.Id, ct);

        if (driver is null)
        {
            return Error.NotFound("Driver.NotFound", $"Driver with ID {request.Id} was not found.");
        }

        driver.UpdateDetails(request.LicenseNumber);
        await _context.SaveChangesAsync(ct);

        var user = await _context.DomainUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == driver.UserId, ct);

        return new DriverDto(
            driver.Id,
            driver.UserId,
            user?.Name?.GetTranslation(_languageContext.Language),
            driver.LicenseNumber,
            driver.Status.ToString(),
            driver.ActiveVehicleId);
    }
}
