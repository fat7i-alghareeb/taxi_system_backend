using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.PaymentMethods;

namespace Taxi.Application.Features.PaymentMethods.Commands.DeletePaymentMethod;

public class DeletePaymentMethodCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IStripePaymentService stripe)
    : IRequestHandler<DeletePaymentMethodCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(DeletePaymentMethodCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var method = await context.PaymentMethods
            .FirstOrDefaultAsync(
                m => m.Id == request.PaymentMethodId && m.PassengerId == userId && m.DeletedAtUtc == null,
                ct);

        if (method is null)
        {
            return PassengerPaymentMethodErrors.NotFound;
        }

        // Detach from Stripe (best-effort) then soft-delete locally.
        await stripe.DetachPaymentMethodAsync(method.GatewayPaymentMethodId, ct);
        method.SoftDelete();

        await context.SaveChangesAsync(ct);
        return Result.Success;
    }
}
