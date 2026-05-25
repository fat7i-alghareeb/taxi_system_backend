# 06. Detailed Entity Analysis

This document provides a class-by-class, property-by-property breakdown of every Domain Entity in the Taxi system to provide maximum granularity.

---

## 1. `User` (Aggregate Root)
**Namespace:** `Taxi.Domain.Users`
**Purpose:** Represents a physical person interacting with the system (Passenger, Driver, or Admin). This entity focuses on domain business logic, while `AppUser` (Identity) focuses on authentication.

### Properties
- `Id` (Guid): Unique identifier. Matches the `AppUser.Id`.
- `Name` (LocalizedText): Trilingual name (English, Arabic, Dutch, etc.).
- `Phone` (string): Unique verified phone number.
- `Email` (string?): Optional email address.
- `Role` (UserRole Enum): `Passenger`, `Driver`, or `Admin`.
- `ProfilePhotoUrl` (string?): Path to locally stored profile picture.
- `FcmToken` (string?): Firebase Cloud Messaging token for push notifications.
- `IsActive` (bool): Soft ban/suspend flag.
- **Inherited:** `CreatedAtUtc`, `CreatedBy`, `LastModifiedUtc`, `LastModifiedBy`, `DeletedAtUtc` (AuditableEntity).

### Methods
- `Create(...)`: Static factory enforcing invariants (e.g., must have a valid role and name).
- `UpdateProfile(string name, string? photoUrl)`: Updates mutable profile fields.
- `UpdateFcmToken(string token)`: Updates the device token for push notifications.
- `AssignVehicle(Guid vehicleId)`: Validates the user is actually a Driver before assigning a physical vehicle.

---

## 2. `Driver` (Aggregate Root)
**Namespace:** `Taxi.Domain.Drivers`
**Purpose:** Specialized profile containing Driver-specific metadata. Tied 1:1 with a `User` where `Role = Driver`.

### Properties
- `Id` (Guid): Unique identifier.
- `UserId` (Guid): Foreign key to the `User`.
- `LicenseNumber` (string): Official driver's license string.
- `Status` (DriverStatus Enum): `Offline`, `Idle` (ready for trips), `OnTrip` (currently busy).
- `ActiveVehicleId` (Guid?): The specific car they are driving *right now*.

### Methods
- `Create(Guid id, Guid userId, string licenseNumber)`: Factory method.
- `UpdateDetails(string licenseNumber)`: Updates license info.
- `SetActiveVehicle(Guid vehicleId)`: Logs the driver into a specific car.
- `UpdateStatus(DriverStatus status)`: State machine transition for the driver's availability.

---

## 3. `VehicleType` (Aggregate Root)
**Namespace:** `Taxi.Domain.Vehicles`
**Purpose:** The business categories of cars (e.g., Standard, Premium, Van) and their exact pricing math.

### Properties
- `Id` (Guid)
- `Code` (string): Unique business identifier (e.g., `STD`, `PRM`, `VAN`).
- `Name` (LocalizedText): e.g., English="Standard", Arabic="Ø¹Ø§Ø¯ÙŠ".
- `PassengerCapacity` (int): Max seats (e.g., 4 or 7).
- `RatePerKm` (decimal): Base price per kilometer driven.
- `RatePerMin` (decimal): Base price per minute driven.
- `MinimumFare` (decimal): Floor price.
- `CurrencyCode` (string): Usually "EUR".
- `IsActive` (bool): If false, passengers cannot select this type.
- `SortOrder` (int): Determines UI rendering order.

### Methods
- `Create(...)`: Factory.
- `UpdatePricing(decimal ratePerKm, decimal ratePerMin, decimal minFare)`: Admin mutation.
- `UpdateSortOrder(int sortOrder)`
- `Activate() / Deactivate()`: Toggles availability.

---

## 4. `Vehicle` (Aggregate Root)
**Namespace:** `Taxi.Domain.Vehicles`
**Purpose:** A physical car attached to a specific Driver.

### Properties
- `Id` (Guid)
- `VehicleTypeId` (Guid): E.g., points to the "Premium" type.
- `DriverId` (Guid): The owner/driver of this physical car.
- `Make` (string): e.g., "Toyota"
- `Model` (string): e.g., "Camry"
- `Year` (string): e.g., "2023"
- `Color` (string): e.g., "White"
- `LicensePlate` (string): Unique plate number.
- `IsActive` (bool)

### Methods
- `Create(...)`: Factory.
- `UpdateDetails(string color, string plate)`
- `Activate() / Deactivate()`

---

## 5. `PricingQuote` (Aggregate Root)
**Namespace:** `Taxi.Domain.Trips`
**Purpose:** Represents a frozen, guaranteed price offer given to a passenger for a specific A-to-B route.

### Properties
- `Id` (Guid)
- `PassengerId` (Guid)
- `VehicleTypeId` (Guid)
- `DistanceKm` (decimal): Snapshot from Google Maps.
- `DurationMin` (decimal): Snapshot from Google Maps.
- `OriginalFare` (decimal): Pre-discount price.
- `FinalFare` (decimal): What Stripe will actually charge.
- `DiscountPercent` (decimal): The applied discount.
- `CurrencyCode` (string)
- `ValidUntilUtc` (DateTime): Always `UtcNow + 15 mins`.
- `Used` (bool): True if this quote was successfully turned into a Trip. Prevents double-booking.

### Methods
- `Create(...)`
- `MarkAsUsed()`: Commits the quote.
- `MarkAsUnused()`: Used by Stripe webhooks to free the quote if the payment explicitly failed.
- `IsExpired()`: `=> DateTime.UtcNow > ValidUntilUtc`

---

## 6. `Trip` (Aggregate Root)
**Namespace:** `Taxi.Domain.Trips`
**Purpose:** The central state machine of the application. Tracks a ride from request to completion.

### Properties
- `Id` (Guid)
- `ReferenceCode` (string): Short human-readable code (e.g., `TRP-1A2B3C`).
- `PassengerId` (Guid)
- `DriverId` (Guid?): Null until a driver accepts/is assigned.
- `VehicleTypeId` (Guid)
- `QuoteId` (Guid)
- `Status` (TripStatus Enum): `AwaitingPayment`, `PendingDriver`, `DriverAssigned`, `DriverEnRoute`, `DriverArrived`, `InProgress`, `Completed`, `Cancelled`, `Refunded`, `PaymentFailed`.
- `ScheduledAtUtc` (DateTimeOffset?): For future rides.
- `Stops` (IReadOnlyList<TripStop>): Owned entity containing Lat/Lng of Pickup, Dropoff, and waypoints.

### Methods (State Machine Transitions)
Every transition method validates that the current state allows moving to the target state. If valid, it mutates the state and raises a `DomainEvent`.
- `Request(...)`: Initial factory. Raises `TripRequested`.
- `ConfirmPayment()`: AwaitingPayment â†’ PendingDriver. Raises `PaymentConfirmed`.
- `MarkPaymentFailed(string reason)`: AwaitingPayment â†’ PaymentFailed. Raises `PaymentFailed`.
- `AssignDriver(Guid driverId)`: PendingDriver â†’ DriverAssigned. Raises `DriverAssigned`.
- `DriverEnRoute()`: DriverAssigned â†’ DriverEnRoute. Raises `DriverEnRoute`.
- `DriverArrived()`: DriverEnRoute â†’ DriverArrived. Raises `DriverArrived`.
- `Start()`: DriverArrived â†’ InProgress. Raises `TripStarted`.
- `Complete()`: InProgress â†’ Completed. Raises `TripCompleted`.
- `Cancel()`: Allows cancellation only if not yet InProgress/Completed. Raises `TripCancelled`.
- `MarkRefunded(decimal amount)`: Completed/Cancelled â†’ Refunded. Raises `TripRefunded`.

---

## 7. `Payment` (Aggregate Root)
**Namespace:** `Taxi.Domain.Payments`
**Purpose:** Tracks financial transactions securely via Stripe IDs.

### Properties
- `Id` (Guid)
- `TripId` (Guid)
- `Amount` (decimal)
- `Currency` (string)
- `Status` (PaymentStatus Enum): `Pending`, `Completed`, `Failed`, `Refunded`.
- `StripePaymentIntentId` (string)
- `StripeClientSecret` (string)
- `StripeChargeId` (string?)
- `FailureCode` (string?)
- `FailureMessage` (string?)

### Methods
- `CreateForStripe(...)`: Factory.
- `MarkAsCompleted(string chargeId)`
- `MarkAsFailed(string code, string message)`
- `MarkAsRefunded()`

---

## 8. `AppConfig` (Aggregate Root)
**Namespace:** `Taxi.Domain.Configuration`
**Purpose:** Key-value store for global settings.

### Properties
- `Key` (string): e.g., "TripDiscountPercent"
- `Value` (string): e.g., "15.5"
- `Description` (string?)
- `UpdatedAtUtc` (DateTime)

### Methods
- `Create(string key, string value)`
- `UpdateValue(string newValue)`
