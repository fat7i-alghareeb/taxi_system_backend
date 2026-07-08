using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.PaymentMethods;

namespace Taxi.Application.Features.PaymentMethods.Commands.SetDefaultPaymentMethod;

public class SetDefaultPaymentMethodCommandHandler(
    IAppDbContext context,
    IUser currentUser)
    : IRequestHandler<SetDefaultPaymentMethodCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(SetDefaultPaymentMethodCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var methods = await context.PaymentMethods
            .Where(m => m.PassengerId == userId && m.DeletedAtUtc == null)
            .ToListAsync(ct);

        var target = methods.FirstOrDefault(m => m.Id == request.PaymentMethodId);
        if (target is null)
        {
            return PassengerPaymentMethodErrors.NotFound;
        }

        foreach (var method in methods)
        {
            if (method.IsDefault && method.Id != target.Id)
            {
                method.UnsetAsDefault();
            }
        }

        target.SetAsDefault();
        await context.SaveChangesAsync(ct);
        return Result.Success;
    }
}
