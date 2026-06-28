using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Audit;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.SuspendPassenger;

public sealed class SuspendPassengerCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<SuspendPassengerCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(SuspendPassengerCommand request, CancellationToken ct)
    {
        var user = await context.DomainUsers.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Error.NotFound(LocalizationKeys.User.NotFound, "User not found.");
        }

        var result = user.Deactivate();
        if (result.IsError)
        {
            return result.Errors;
        }

        Guid? adminId = Guid.TryParse(currentUser.Id, out var id) ? id : null;
        var audit = AuditLog.Create(
            Guid.NewGuid(),
            adminId,
            action: "SuspendedPassenger",
            entityName: "User",
            entityId: user.Id.ToString(),
            oldValue: null,
            newValue: string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim());
        if (audit.IsSuccess)
        {
            context.AuditLogs.Add(audit.Value);
        }

        await context.SaveChangesAsync(ct);
        return Result.Success;
    }
}
