using MediatR;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.ResendArrivedNotification;

public record ResendArrivedNotificationCommand(Guid TripId) : IRequest<Result<Success>>;
