using MediatR;
using Taxi.Application.Features.PaymentMethods.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.PaymentMethods.Queries.GetPaymentMethods;

/// <summary>Returns the current passenger's saved reusable payment methods (default first).</summary>
public record GetPaymentMethodsQuery : IRequest<Result<List<PaymentMethodDto>>>;
