using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Audit;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Users.Commands.ReactivatePassenger;

public sealed class ReactivatePassengerCommandHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<ReactivatePassengerCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ReactivatePassengerCommand request, CancellationToken ct)
    {
        var user = await context.DomainUsers.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Error.NotFound(LocalizationKeys.User.NotFound, "User not found.");
        }

        var result = user.Activate();
        if (result.IsError)
        {
            return result.Errors;
        }

        Guid? adminId = Guid.TryParse(currentUser.Id, out var id) ? id : null;
        var audit = AuditLog.Create(
            Guid.NewGuid(),
            adminId,
            action: "ReactivatedPassenger",
            entityName: "User",
            entityId: user.Id.ToString(),
            oldValue: null,
            newValue: null);
        if (audit.IsSuccess)
        {
            context.AuditLogs.Add(audit.Value);
        }

        await context.SaveChangesAsync(ct);
        return Result.Success;
    }
}
