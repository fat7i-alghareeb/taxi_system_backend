using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Drivers.Commands.DeleteDriver;

public class DeleteDriverCommandHandler(
    IAppDbContext context) : IRequestHandler<DeleteDriverCommand, Result<Deleted>>
{
    private readonly IAppDbContext _context = context;

    public async Task<Result<Deleted>> Handle(DeleteDriverCommand request, CancellationToken ct)
    {
        var driver = await _context.Drivers
            .FirstOrDefaultAsync(d => d.Id == request.Id, ct);

        if (driver is null)
        {
            return Error.NotFound("Driver.NotFound", $"Driver with ID {request.Id} was not found.");
        }

        driver.SoftDelete();
        await _context.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}
