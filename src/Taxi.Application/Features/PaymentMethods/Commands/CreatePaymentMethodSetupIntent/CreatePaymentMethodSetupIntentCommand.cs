using MediatR;
using Taxi.Application.Features.PaymentMethods.Dtos;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.PaymentMethods.Commands.CreatePaymentMethodSetupIntent;

/// <summary>Starts a Stripe SetupIntent so the passenger can save a reusable payment method.</summary>
public record CreatePaymentMethodSetupIntentCommand : IRequest<Result<PaymentMethodSetupDto>>;
