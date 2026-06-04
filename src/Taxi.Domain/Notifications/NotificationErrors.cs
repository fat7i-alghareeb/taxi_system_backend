using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;

namespace Taxi.Domain.Notifications;

public static class NotificationErrors
{
    public static readonly Error TitleRequired = Error.Validation(
        code: LocalizationKeys.Notification.TitleRequired,
        description: "Notification title is required.");

    public static readonly Error BodyRequired = Error.Validation(
        code: LocalizationKeys.Notification.BodyRequired,
        description: "Notification body is required.");
}
