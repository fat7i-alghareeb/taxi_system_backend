using MediatR;

using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Payments.Commands.HandleStripeWebhook;

public record HandleStripeWebhookCommand(string Json, string Signature) : IRequest<Result<Success>>;
