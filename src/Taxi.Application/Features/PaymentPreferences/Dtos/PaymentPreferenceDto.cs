namespace Taxi.Application.Features.PaymentPreferences.Dtos;

/// <summary>
/// The passenger's preferred trip-booking method (or null) plus the enabled method types the app
/// can offer. This preference is for new trip bookings only — it does not drive automatic fees.
/// </summary>
public record PaymentPreferenceDto(
    string? PreferredMethodType,
    IReadOnlyList<string> EnabledMethodTypes);
