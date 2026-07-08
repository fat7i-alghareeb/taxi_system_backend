using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.PaymentMethods.Commands.DeletePaymentMethod;

public record DeletePaymentMethodCommand(Guid PaymentMethodId) : IRequest<Result<Success>>;
