using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.PaymentMethods;

namespace Taxi.Application.Features.PaymentPreferences.Commands.SetPaymentPreference;

public class SetPaymentPreferenceCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    IOptions<PaymentPreferenceOptions> options)
    : IRequestHandler<SetPaymentPreferenceCommand, Result<Success>>
{
    private readonly PaymentPreferenceOptions options = options.Value;

    public async Task<Result<Success>> Handle(SetPaymentPreferenceCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var normalized = string.IsNullOrWhiteSpace(request.PreferredMethodType)
            ? null
            : request.PreferredMethodType.Trim().ToLowerInvariant();

        if (normalized is not null
            && !options.EnabledMethodTypes.Any(t => string.Equals(t, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return PassengerPaymentMethodErrors.PreferredMethodInvalid;
        }

        var user = await context.DomainUsers.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound(LocalizationKeys.Auth.UserNotFound, "User not found.");
        }

        user.SetPreferredPaymentMethodType(normalized);
        await context.SaveChangesAsync(ct);
        return Result.Success;
    }
}
