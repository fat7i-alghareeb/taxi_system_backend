using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Drivers;

namespace Taxi.Application.Features.Drivers.Commands.SuspendDriver;

public class SuspendDriverCommandHandler(IAppDbContext context)
    : IRequestHandler<SuspendDriverCommand, Result<Success>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Success>> Handle(SuspendDriverCommand request, CancellationToken ct)
    {
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId, ct);
        if (driver == null)
        {
            return Result.Failure<Success>(Error.NotFound(LocalizationKeys.Driver.NotFound, "Driver profile not found."));
        }

        var suspendResult = driver.Suspend();
        if (suspendResult.IsFailure)
        {
            return suspendResult.Error;
        }

        await _context.SaveChangesAsync(ct);

        return Result.Success;
    }
}
