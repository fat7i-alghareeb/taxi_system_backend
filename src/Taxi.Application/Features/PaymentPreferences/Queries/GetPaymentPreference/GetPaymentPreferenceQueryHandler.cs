using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Options;
using Taxi.Application.Features.PaymentPreferences.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.PaymentPreferences.Queries.GetPaymentPreference;

public class GetPaymentPreferenceQueryHandler(
    IAppDbContext context,
    IUser currentUser,
    IOptions<PaymentPreferenceOptions> options)
    : IRequestHandler<GetPaymentPreferenceQuery, Result<PaymentPreferenceDto>>
{
    private readonly PaymentPreferenceOptions options = options.Value;

    public async Task<Result<PaymentPreferenceDto>> Handle(GetPaymentPreferenceQuery request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var user = await context.DomainUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return Error.NotFound(LocalizationKeys.Auth.UserNotFound, "User not found.");
        }

        return new PaymentPreferenceDto(user.PreferredPaymentMethodType, options.EnabledMethodTypes);
    }
}
