# 02. Infrastructure Layer (`Taxi.Infrastructure`) Exhaustive Analysis

The Infrastructure layer implements the abstract interfaces (`IAppDbContext`, `ITripNotifier`, `IFirebaseAuthService`, etc.) defined in the Application layer. It wires the pure domain logic to physical databases, external APIs, and real-time sockets.

---

## 1. Persistence & Entity Framework Core

### Database Context (`AppDbContext`)
- Inherits from ASP.NET Identity's `IdentityDbContext<AppUser>` to blend Microsoft's authentication tables with the custom Taxi domain tables.
- Implements `IAppDbContext` to expose `DbSet<T>` for all Aggregates.

### Entity Configurations (`Data/Configurations/`)
- **JSONB Mappings**: The `LocalizedText` value object is heavily mapped to PostgreSQL JSONB columns using EF Core 8's `builder.OwnsOne(x => x.Name, n => n.ToJson());`. This allows blazing-fast querying of specific languages without table joins.
- **Global Query Filters**: Every entity inheriting from `AuditableEntity` receives an automatic soft-delete filter: `builder.HasQueryFilter(x => x.DeletedAtUtc == null);`. This ensures deleted rows are never accidentally fetched.
- **Precision**: Money fields (Fares, Discounts) are strictly typed to `HasPrecision(18, 2)` or `HasPrecision(10, 2)`. Coordinates are typed to `HasPrecision(18, 10)` for pinpoint GPS accuracy.

### EF Core Interceptors
The application uses two powerful interceptors to offload cross-cutting concerns from the business logic:
1.  **`AuditableEntityInterceptor`**: 
    - Hooks into `SavingChangesAsync`.
    - Automatically injects `CreatedAtUtc`, `CreatedBy`, `LastModifiedUtc`, and `LastModifiedBy` based on the currently authenticated `IUser`.
    - Recursively checks `OwnedEntities` to stamp nested value objects.
2.  **`AuditLogInterceptor`**: 
    - Analyzes the `ChangeTracker` before saving.
    - For every Modified, Added, or Deleted entity, it creates a new `AuditLog` row containing the User ID, Action, Entity Name, and the Entity ID.

### Database Initialization
- **`AppDbContextInitializer`**: Automatically runs migrations on startup (`ApplyMigrationsAsync`).
- **Seeding**: Automatically seeds the system with a `SuperAdmin` user, an `AdminDriver`, and three default `VehicleType` configurations (Standard, Premium, Van).

---

## 2. Authentication & Identity Engine

Authentication relies on a secure **two-tier architecture**: Firebase for initial mobile verification, and stateless JWTs for backend sessions.

### `FirebaseAuthService`
- Decodes Google Firebase JWTs sent by the mobile app after SMS verification.
- Extracts the `phone_number` claim to prove the user actually owns the phone number.

### `IdentityService`
- Wrapper around ASP.NET `UserManager<AppUser>`.
- **`GetOrCreateUserByPhoneAsync`**: Core login flow. If a user doesn't exist, it auto-generates a new `AppUser` using their phone number, assigns an impossible placeholder password, and issues an ID.

### `TokenProvider`
- Generates the backend's official JSON Web Tokens (JWT).
- **Refresh Token Rotation**: Standard JWTs expire in minutes. To keep mobile users logged in indefinitely, a cryptographically secure `RefreshToken` (32-byte Base64) is generated, saved to the database (`context.RefreshTokens`), and returned. When the access token expires, the client swaps the Refresh Token for a new pair.

---

## 3. Real-Time Sockets (`SignalR`)

The application replaces heavy HTTP polling with instantaneous WebSocket push notifications.

### `TripHub`
- The core WebSocket hub hosted at `/hubs/trips`.
- **Connection Groups**: Instead of broadcasting to everyone, connections are mapped to targeted groups:
  - `User_{userId}`: Personal updates.
  - `Trip_{tripId}`: Updates bound to a specific ongoing trip.
  - `VehicleType_{vehicleTypeCode}`: Used by drivers idling on the map to receive new trip broadcasts tailored to their car type.

### `SignalRTripNotifier`
- Implements `ITripNotifier`. It is called by the Application layer's Domain Event Handlers.
- It pushes strongly-typed JSON records (`TripRequestedNotification`, `DriverAssignedNotification`, etc.) strictly to the specific `Group` involved, ensuring drivers don't see other drivers' data.

---

## 4. Third-Party Integrations

### Google Maps (`GoogleMapsService` & `GoogleGeocodingService`)
- Implements `IDirectionsService` and `IGeocodingService` using `HttpClient`.
- **Directions API**: Calculates distances, durations, and returns an `EncodedPolyline`. It elegantly handles multi-stop trips via the `&waypoints=` query string. It uses a custom `PolylineHelper` to decode and re-encode the Google strings to prevent map distortion on the client.
- **Geocoding API**: Translates raw GPS Coordinates into Human-readable strings (e.g., "Main Street 123"). It features a fallback mechanism to translate "Plus Codes" (e.g., 54PH+PM9) into proper neighborhood names.

### Stripe Payments (`StripePaymentService`)
- Orchestrates credit card processing securely without touching raw card data.
- **`CreatePaymentIntentAsync`**: Sets up an intent with `CaptureMethod = "automatic"`. Injects the `TripId` and `PassengerId` into the Stripe `Metadata` for tracking.
- **`StripeWebhookValidator`**: The backbone of the billing system. It takes raw JSON from Stripe, validates the cryptographic signature using the `WebhookSecret`, and translates it into internal `StripeWebhookEvent` records (`PaymentIntentSucceeded`, `PaymentIntentFailed`, `ChargeRefunded`).

---

## 5. Local Storage
- **`LocalFileStorage`**: Implements `IFileStorage`. Manages uploading user profile pictures to the local server `wwwroot` directory. Generates and normalizes relative paths for direct web access.
