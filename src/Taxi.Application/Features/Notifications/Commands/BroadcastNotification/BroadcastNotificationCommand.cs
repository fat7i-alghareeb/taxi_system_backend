using MediatR;
using Taxi.Contracts.Notifications;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Notifications.Commands.BroadcastNotification;

public record BroadcastNotificationCommand(
    NotificationAudience Audience,
    string Title,
    string Body,
    Dictionary<string, string>? Data = null) : IRequest<Result<Success>>;
