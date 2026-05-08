namespace Taxi.Contracts.Responses.Trips;

public record TripResponse(
    Guid Id,
    string ReferenceCode,
    Guid PassengerId,
    Guid? DriverId,
    Guid VehicleTypeId,
    string Status,
    decimal QuotedFare,
    string CurrencyCode,
    DateTime CreatedAtUtc);

