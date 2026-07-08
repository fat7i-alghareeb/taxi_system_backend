using MediatR;
using Taxi.Application.Features.PaymentPreferences.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.PaymentPreferences.Queries.GetPaymentPreference;

/// <summary>Returns the passenger's preferred booking method + the enabled method types.</summary>
public record GetPaymentPreferenceQuery : IRequest<Result<PaymentPreferenceDto>>;
