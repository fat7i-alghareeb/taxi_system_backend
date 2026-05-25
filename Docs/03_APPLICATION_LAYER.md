# 03. Application Layer (`Taxi.Application`) Exhaustive Analysis

The Application layer implements the **Clean Architecture Use Cases**. It acts as the orchestrator: catching requests from the API, coordinating Domain logic, and commanding the Infrastructure layer via abstract interfaces. 

It heavily utilizes the **CQRS (Command Query Responsibility Segregation)** pattern via `MediatR`. Every operation is either a Command (mutates state) or a Query (reads state).

---

## 1. Core Abstractions & Behaviors

### Cross-Cutting Behaviors (MediatR Pipeline)
- **`ValidationBehavior`**: Intercepts every incoming request. It runs all registered FluentValidation `IValidator<TRequest>` instances. If validation fails, it short-circuits the pipeline and immediately returns an `Error.Validation` result, keeping controllers pure.
- **`LoggingBehavior`**: Automatically logs the start, end, and execution time of every command/query.
- **`QueryCachingBehavior`**: Queries implementing the marker interface `ICachedQuery` are intercepted. The behavior checks the `ICacheService` (Redis/Memory) before hitting the database, drastically improving read speeds for static data like Vehicle Catalogs.

---

## 2. Feature: Auth & Identity

### Authentication Flow
- **`LoginCommand`**: The entry point for mobile apps. It expects a `FirebaseIdToken` (proving phone number ownership) and an optional `FcmToken` (for push notifications). It uses `IFirebaseAuthService` to decode the token, syncs the phone number to the Domain `User` (and `AppUser`), and finally uses `ITokenProvider` to issue a backend Access + Refresh token pair.
- **`RefreshTokenQuery`**: Handles long-lived sessions. Takes an expired access token and a refresh token, validates them against the database (`context.RefreshTokens`), and issues a new pair.
- **`RegisterAdminCommand`**: Separate registration flow used only for backend admins. It creates both the Identity `AppUser` (with password) and the Domain `User` simultaneously.

---

## 3. Feature: Trips (The Core Engine)

The Trip lifecycle is entirely managed by MediatR Commands:

### 1. `GetPricingQuotesCommand`
- **Input**: A list of `Latitude/Longitude` stops.
- **Process**:
  1. Calls `IDirectionsService` (Google Maps) to get total distance and duration.
  2. Fetches all active `VehicleType`s from the DB.
  3. Fetches the active `TripDiscountPercent` config.
  4. Calls the Domain `IPricingService` to calculate base fares per vehicle.
  5. Generates a list of `PricingQuote` domain entities, valid for 15 minutes, and saves them to the DB.
- **Output**: A DTO list of vehicle options, capacities, and calculated fares.

### 2. `RequestTripCommand`
- **Input**: The chosen `QuoteId` and the `Stops`.
- **Process**:
  1. Validates the Quote (ensures it's not expired or already used).
  2. Creates the `Trip` aggregate root (which begins in `TripStatus.AwaitingPayment`).
  3. Converts DTO stops into Domain `TripStop` value objects.
  4. If Stripe is disabled: Instantly marks payment confirmed and calls `TripDispatchHelper` to auto-assign a fallback driver (legacy flow).
  5. If Stripe is enabled: Calls `IStripePaymentService.CreatePaymentIntentAsync`. Creates a Domain `Payment` entity linked to the Trip.
- **Output**: The Trip DTO containing the Stripe `ClientSecret` so the mobile app can render the PaymentSheet.

### 3. `CancelTripCommand`
- Handles passenger-initiated cancellations. Validates that the trip isn't already completed/started.
- **Stripe Orchestration**: 
  - If the trip was still `AwaitingPayment`, it actively cancels the Stripe Payment Intent to prevent phantom holds.
  - If the trip had a completed payment (e.g., `DriverAssigned`), it calls Stripe to issue an automatic refund.

### 4. Domain Event Handlers
- The domain raises abstract events (e.g., `TripStarted`, `DriverAssigned`, `PaymentFailed`).
- The application layer catches these via MediatR `INotificationHandler`.
- Handlers like `DriverAssignedEventHandler` immediately pipe these events into the `ITripNotifier` (SignalR), pushing the state change to the mobile apps in real-time.

---

## 4. Feature: Payments

### `HandleStripeWebhookCommand`
- The most critical background command. It receives the raw Stripe JSON payload from the API controller.
- It parses the event via `IStripeWebhookValidator`.
- **PaymentIntentSucceeded**: Finds the matching `Payment` entity. Transitions Trip to `PendingDriver` (by calling `Trip.ConfirmPayment()`). Finally, it triggers `TripDispatchHelper.AssignDefaultDriverAsync` to wake up a driver.
- **PaymentIntentFailed**: Transitions Trip to `PaymentFailed` and explicitly un-consumes the `PricingQuote` so the passenger can try another card without re-requesting a quote.

---

## 5. Feature: Maps & Geocoding

- **`GetDirectionsQuery`**: Wraps the `IDirectionsService`. Returns the `EncodedPolyline` strings needed by Flutter to draw the blue route line on the map.
- **`SearchPlacesQuery` & `ReverseGeocodeQuery`**: Powers the location autocomplete text fields and the "Where am I?" pin-drop feature.

---

## 6. Feature: Vehicles & Drivers

Standard CQRS CRUD operations powering the Admin Dashboard:
- **Vehicle Types**: Create/Update base vehicles (Standard, Premium, Van). Sets capacities and `RatePerKm` pricing models.
- **Drivers & Vehicles**: Creating a Driver profile linked to a User, creating specific physical Vehicles (Make/Model/Plate), and assigning a Vehicle to a Driver (`AssignVehicleCommand`).

### Projections & Localization
Queries like `GetVehicleCatalogQuery` are fully localized. Instead of loading the massive JSONB dictionary, the query intercepts the current HTTP `Accept-Language` header (via `ILanguageContext`) and dynamically projects only the requested string:
`lang == Languages.Ar ? t.Name.Ar : t.Name.En`

---

## 7. Feature: Configuration & Users

- **`UpdateTripDiscountCommand`**: Admin command to set a global percentage discount across all fares (stored in `AppConfig` table).
- **`UpdateUserProfileCommand`**: Allows passengers/drivers to update their Name. Also handles multipart form photo uploads, piping the `FileStream` directly into the `IFileStorage` provider.
