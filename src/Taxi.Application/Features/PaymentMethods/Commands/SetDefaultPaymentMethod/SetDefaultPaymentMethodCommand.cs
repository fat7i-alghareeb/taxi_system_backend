using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.PaymentMethods.Commands.SetDefaultPaymentMethod;

public record SetDefaultPaymentMethodCommand(Guid PaymentMethodId) : IRequest<Result<Success>>;
