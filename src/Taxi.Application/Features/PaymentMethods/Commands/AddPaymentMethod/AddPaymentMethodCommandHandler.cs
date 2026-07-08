using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.PaymentMethods.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.PaymentMethods;

namespace Taxi.Application.Features.PaymentMethods.Commands.AddPaymentMethod;

public class AddPaymentMethodCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IStripePaymentService stripe)
    : IRequestHandler<AddPaymentMethodCommand, Result<PaymentMethodDto>>
{
    public async Task<Result<PaymentMethodDto>> Handle(AddPaymentMethodCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var existing = await context.PaymentMethods
            .Where(m => m.PassengerId == userId && m.DeletedAtUtc == null)
            .ToListAsync(ct);

        // Idempotent: re-adding an already-saved method returns it (optionally promoting to default).
        var duplicate = existing.FirstOrDefault(m => m.GatewayPaymentMethodId == request.PaymentMethodId);
        if (duplicate is not null)
        {
            if (request.SetAsDefault && !duplicate.IsDefault)
            {
                SetSingleDefault(existing, duplicate);
                await context.SaveChangesAsync(ct);
            }

            return duplicate.ToDto();
        }

        var detailsResult = await stripe.GetPaymentMethodDetailsAsync(request.PaymentMethodId, ct);
        if (detailsResult.IsFailure)
        {
            return detailsResult.Error;
        }

        var details = detailsResult.Value;

        var createResult = PassengerPaymentMethod.Create(
            Guid.NewGuid(),
            userId,
            request.PaymentMethodId,
            details.Brand,
            details.LastFour,
            details.ExpiryMonth,
            details.ExpiryYear,
            details.CardholderName);

        if (createResult.IsFailure)
        {
            return createResult.Error;
        }

        var method = createResult.Value;
        context.PaymentMethods.Add(method);

        // The first saved method, or one explicitly requested, becomes the default reusable
        // method used for automatic ride-related charges.
        var shouldBeDefault = request.SetAsDefault || existing.All(m => !m.IsDefault);
        if (shouldBeDefault)
        {
            SetSingleDefault(existing, method);
        }

        await context.SaveChangesAsync(ct);
        return method.ToDto();
    }

    private static void SetSingleDefault(IEnumerable<PassengerPaymentMethod> existing, PassengerPaymentMethod newDefault)
    {
        foreach (var other in existing)
        {
            if (other.IsDefault)
            {
                other.UnsetAsDefault();
            }
        }

        newDefault.SetAsDefault();
    }
}
