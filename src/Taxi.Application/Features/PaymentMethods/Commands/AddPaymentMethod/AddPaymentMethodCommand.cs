using MediatR;
using Taxi.Application.Features.PaymentMethods.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.PaymentMethods.Commands.AddPaymentMethod;

/// <summary>Persists a reusable payment method after the SetupIntent is confirmed in the app.</summary>
public record AddPaymentMethodCommand(string PaymentMethodId, bool SetAsDefault)
    : IRequest<Result<PaymentMethodDto>>;
