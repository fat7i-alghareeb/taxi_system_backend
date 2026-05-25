# Full ASP.NET Core Backend Architecture Analysis (Customer Taxi API)

## 1. High-Level Architecture Overview

The backend uses a strict implementation of **Clean Architecture** combined with **Domain-Driven Design (DDD)** and **CQRS (Command Query Responsibility Segregation)**. It is heavily optimized for PostgreSQL using JSONB columns for multi-language support, integrates Firebase for SMS auth, Stripe for payments, and SignalR for real-time dispatching.

The project is divided into four main layers:
1. **Taxi.Domain**: Pure business rules and aggregates. No external dependencies.
2. **Taxi.Application**: Use cases (CQRS), MediatR behaviors, and port interfaces.
3. **Taxi.Infrastructure**: EF Core, Identity, SignalR, Firebase, and 3rd-party integrations.
4. **Taxi.Api**: HTTP endpoints, versioning, localization, and rate limiting.

---

## 2. Domain Layer (`Taxi.Domain`)

The Domain layer is purely focused on business logic. It bans the use of exceptions for control flow, opting instead for a highly robust `Result<T>` pattern.

### Key Base Classes & Patterns
*   **`Entity` & `AuditableEntity`**: All domain models inherit from `Entity`, which handles identity (`Guid Id`) and queues `DomainEvent` objects. `AuditableEntity` adds `CreatedAtUtc`, `CreatedBy`, `LastModifiedUtc`, and `LastModifiedBy`.
*   **`Result<T>` & `Error`**: A custom Result pattern is enforced. Operations return `Result.Success` or strongly-typed errors like `Error.NotFound()` or `Error.Validation()`. `ErrorKind` enumerates types: Failure, Validation, Conflict, NotFound, Unauthorized, Forbidden.
*   **`ValueObject` (`LocalizedText`)**: A critical feature. All text that must be translated (e.g., Vehicle names, User names) uses `LocalizedText`. It holds 9 languages (En, Ar, Nl, De, Pl, Uk, Fr, Es, Ro) and is natively mapped by EF Core into a single PostgreSQL `JSONB` column.
*   **Domain Events**: Extending `INotification`, events (e.g., `TripRequested`) are added to an entity's internal list and published by EF Core right before `SaveChanges` completes.

### Core Aggregates
*   **`User`**: Tracks passengers and admins using `LocalizedText` for names.
*   **`Driver` & `Vehicle`**: Tracks `DriverStatus` (Offline, Online, OnTrip) and GPS location. `VehicleType` manages pricing variables (`RatePerKm`, `RatePerMin`, `BaseFare`).
*   **`Trip` & `PricingQuote`**: `Trip` is an immense state machine (PendingQuote -> Scheduled -> DriverAssigned -> InProgress -> Completed). `PricingQuote` locks in a fare for 15 minutes before the trip begins.
*   **`AuditLog`**: Dedicated entity to trace system modifications.

---

## 3. Application Layer (`Taxi.Application`)

The engine room of the application, orchestrating Domain rules using MediatR. It follows the **Vertical Slice** pattern, grouping Commands, Queries, and Handlers by feature (e.g., `Features/Auth/Commands/Login`).

### MediatR Pipeline Behaviors
The CQRS pipeline handles cross-cutting concerns silently:
1.  **`UnhandledExceptionBehaviour`**: Catches and logs hard crashes globally.
2.  **`ValidationBehavior`**: Intercepts requests, runs `FluentValidation` rules, and auto-returns `Result<T>` with `Error.ValidationForProperty` if invalid. Handlers never see invalid data.
3.  **`CachingBehavior`**: Uses .NET 9's new `HybridCache` (L1/L2). Queries implementing `ICachedQuery` are cached automatically. Extremely smart feature: if `IsCultureAware` is true, it splits cache keys by the active language (e.g., `CacheKey_Ar`) to prevent Arabic users from seeing English cached payloads.
4.  **`PerformanceBehaviour`**: Triggers a warning log if any handler takes > 500ms.

### Interfaces (Ports)
The Application layer defines what it needs from the outside world:
*   `IFirebaseAuthService`: To verify Firebase ID tokens for SMS login.
*   `ITripNotifier`: Abstract real-time engine to push updates to apps.
*   `IStripePaymentService`: To manage PaymentIntents and Refunds.
*   `IDirectionsService` / `IGeocodingService`: Maps API abstractions.

---

## 4. Infrastructure Layer (`Taxi.Infrastructure`)

Implements the Application interfaces and connects to physical infrastructure.

### Entity Framework Core & PostgreSQL
*   **`AppDbContext`**: Inherits from ASP.NET Identity's `IdentityDbContext<AppUser>` to blend Microsoft auth with custom Domain tables.
*   **JSONB Mappings**: `LocalizedText` is mapped via `OwnsOne().ToJson()`.
*   **Global Query Filters**: Soft deletion is enforced automatically (`HasQueryFilter(e => e.DeletedAtUtc == null)`).
*   **Interceptors**: 
    *   `AuditableEntityInterceptor`: Automatically stamps `CreatedAtUtc` and user IDs upon save.
    *   `AuditLogInterceptor`: Automatically serializes old/new values into `AuditLog` rows upon entity mutation.
*   **Database Initialiser**: Contains `ApplicationDbContextInitialiser` for applying migrations and seeding the Super Admin, Admin Driver, and Base Vehicle Types.

### Identity & Authentication
*   **Two-Tier Auth**: 
    1. Mobile app proves phone ownership via Firebase SMS. `FirebaseAuthService` verifies the ID token.
    2. Backend creates/fetches a Microsoft `AppUser`, then generates its own stateless `JWT` via `TokenProvider`.
*   **Long-Running Sessions**: Uses secure database-backed `RefreshToken` entities to maintain login indefinitely without exposing long-lived access tokens.

### Integrations
*   **SignalR (`TripHub`)**: Real-time websocket hub. `SignalRTripNotifier` sends strongly-typed JSON records directly to specific connection groups (e.g., pushing `DriverAssignedNotification` to the passenger).
*   **Google Maps (`GoogleMapsService`)**: Uses `HttpClient` to call Directions API. Handles multi-stop trips and utilizes a custom `PolylineHelper` to decode Google's `EncodedPolyline` format back into lat/lng lists.
*   **Stripe**: Secure webhook validation via `StripeWebhookValidator`. Manages idempotency of payments.

---

## 5. API Layer (`Taxi.Api`)

The presentation interface, designed for extremely high resilience and compliance.

### Configurations
*   **API Versioning**: Implemented via `Asp.Versioning` (`api/v1/auth`).
*   **Custom Problem Details**: Unifies all API errors into RFC 7807 JSON formats.
*   **Global Rate Limiting**: `SlidingWindowLimiter` allows 100 requests per minute per IP.
*   **Localization**: `AddAppLocalization()` parses the `Accept-Language` header, injecting `ILanguageContext` into the Application layer, enabling EF Core to project specific JSONB keys dynamically on the database level.
*   **Forwarded Headers**: Securely resolves client IPs behind NGINX/Docker proxies using `ForwardedHeadersOptions` (essential for Rate Limiting accuracy).

### Typical Request Flow (Example: RequestTrip)
1.  **API**: `POST api/v1/trips` receives HTTP request.
2.  **API**: Rate Limiter and JWT Middleware authorize the request.
3.  **Application (Behavior)**: `ValidationBehavior` ensures stops are valid.
4.  **Application (Handler)**: `RequestTripCommandHandler` fetches `PricingQuote`.
5.  **Domain**: Calls `Trip.Request(...)` to enforce state transition. An event is queued.
6.  **Infrastructure (EF)**: Saves to DB. Interceptor stamps audit logs. Event is published.
7.  **Application (Event Handler)**: Intercepts `TripRequestedEvent`, fires `ITripNotifier`.
8.  **Infrastructure (SignalR)**: Pushes `TripRequestedNotification` to driver sockets.
9.  **API**: Returns `201 Created` with the Trip DTO.
