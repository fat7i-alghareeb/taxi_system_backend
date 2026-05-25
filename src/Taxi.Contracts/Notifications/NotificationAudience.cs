namespace Taxi.Contracts.Notifications;

/// <summary>
/// Defines the specific, secure target audience groups allowed for push broadcasts.
/// The backend maps these typed enums internally to secure FCM topic names.
/// </summary>
public enum NotificationAudience
{
    Customers = 0,
    Drivers = 1,
    Admins = 2
}
