using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Drivers.Dtos;
using Taxi.Domain.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;
using Taxi.Domain.Users;

namespace Taxi.Application.Features.Drivers.Commands.CreateDriver;

public class CreateDriverCommandHandler(
    IAppDbContext context) : IRequestHandler<CreateDriverCommand, Result<DriverDto>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<DriverDto>> Handle(CreateDriverCommand request, CancellationToken ct)
    {
        // Check if user exists by phone
        var user = await _context.DomainUsers.FirstOrDefaultAsync(u => u.Phone == request.Phone, ct);

        if (user is null)
        {
            var userResult = User.Create(
                Guid.NewGuid(),
                request.NameEn,
                request.NameAr,
                request.NameEn, // Use EN as NL fallback
                request.Phone,
                null,
                UserRole.Driver);

            if (userResult.IsFailure)
            {
                return userResult.Error;
            }

            user = userResult.Value;
            _context.DomainUsers.Add(user);
        }
        else
        {
            // If user exists, ensure they are not already a driver
            var alreadyDriver = await _context.Drivers.AnyAsync(d => d.UserId == user.Id, ct);
            if (alreadyDriver)
            {
                return Error.Validation("Driver.AlreadyExists", "This user is already a driver.");
            }
        }

        var driverResult = Driver.Create(Guid.NewGuid(), user.Id, request.LicenseNumber);

        if (driverResult.IsFailure)
        {
            return driverResult.Error;
        }

        var driver = driverResult.Value;

        _context.Drivers.Add(driver);
        await _context.SaveChangesAsync(ct);

        return new DriverDto(
            driver.Id,
            driver.UserId,
            user.Name?.En,
            driver.LicenseNumber,
            driver.Status.ToString(),
            driver.ActiveVehicleId);
    }
}
