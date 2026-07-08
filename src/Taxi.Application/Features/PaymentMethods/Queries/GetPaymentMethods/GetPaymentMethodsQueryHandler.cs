using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.PaymentMethods.Dtos;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.PaymentMethods.Queries.GetPaymentMethods;

public class GetPaymentMethodsQueryHandler(
    IAppDbContext context,
    IUser currentUser)
    : IRequestHandler<GetPaymentMethodsQuery, Result<List<PaymentMethodDto>>>
{
    public async Task<Result<List<PaymentMethodDto>>> Handle(GetPaymentMethodsQuery request, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.Id, out var userId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var methods = await context.PaymentMethods
            .AsNoTracking()
            .Where(m => m.PassengerId == userId && m.DeletedAtUtc == null)
            .OrderByDescending(m => m.IsDefault)
            .ThenByDescending(m => m.CreatedAtUtc)
            .Select(m => new PaymentMethodDto(
                m.Id,
                m.CardBrand,
                m.LastFour,
                m.ExpiryMonth,
                m.ExpiryYear,
                m.CardholderName,
                m.IsDefault))
            .ToListAsync(ct);

        return methods;
    }
}
