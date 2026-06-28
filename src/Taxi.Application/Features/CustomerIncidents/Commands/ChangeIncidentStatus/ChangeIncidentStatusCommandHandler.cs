using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.CustomerIncidents.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.CustomerIncidents;

namespace Taxi.Application.Features.CustomerIncidents.Commands.ChangeIncidentStatus;

public sealed class ChangeIncidentStatusCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<ChangeIncidentStatusCommand, Result<CustomerIncidentDto>>
{
    public async Task<Result<CustomerIncidentDto>> Handle(ChangeIncidentStatusCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<CustomerIncidentStatus>(request.Status, ignoreCase: true, out var newStatus))
        {
            return CustomerIncidentErrors.InvalidStatus;
        }

        if (!Guid.TryParse(currentUser.Id, out var adminId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var incident = await context.CustomerIncidents.FirstOrDefaultAsync(i => i.Id == request.IncidentId, ct);
        if (incident is null)
        {
            return CustomerIncidentErrors.NotFound;
        }

        var changeResult = incident.ChangeStatus(newStatus, adminId, request.Note);
        if (changeResult.IsError)
        {
            return changeResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        var passengerName = await context.DomainUsers
            .AsNoTracking()
            .Where(u => u.Id == incident.PassengerId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync(ct);

        return incident.ToDto(passengerName);
    }
}
