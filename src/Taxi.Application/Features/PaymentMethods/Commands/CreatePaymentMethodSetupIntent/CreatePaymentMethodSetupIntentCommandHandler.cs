using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.PaymentMethods.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Payments;

namespace Taxi.Application.Features.PaymentMethods.Commands.CreatePaymentMethodSetupIntent;

public class CreatePaymentMethodSetupIntentCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IClientConfigProvider clientConfig,
    IStripePaymentService stripe)
    : IRequestHandler<CreatePaymentMethodSetupIntentCommand, Result<PaymentMethodSetupDto>>
{
    public async Task<Result<PaymentMethodSetupDto>> Handle(
        CreatePaymentMethodSetupIntentCommand request,
        CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        if (!clientConfig.GetClientConfig().StripeEnabled)
        {
            return PaymentErrors.StripeInitiationFailed;
        }

        var user = await context.DomainUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound(LocalizationKeys.Auth.UserNotFound, "User not found.");
        }

        var setupResult = await stripe.CreateSetupIntentAsync(
            userId,
            user.StripeCustomerId,
            user.Email,
            user.Phone,
            user.Name,
            ct);

        if (setupResult.IsFailure)
        {
            return setupResult.Error;
        }

        var setup = setupResult.Value;

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            user.SetStripeCustomerId(setup.CustomerId);
            await context.SaveChangesAsync(ct);
        }

        return new PaymentMethodSetupDto(
            setup.SetupIntentId,
            setup.ClientSecret,
            setup.PublishableKey,
            setup.CustomerId,
            setup.EphemeralKeySecret);
    }
}
