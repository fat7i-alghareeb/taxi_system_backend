# 08. Required Backend Modifications (`TAXI_SERVER`)

# 08. Required Backend Modifications (`TAXI_SERVER`) — **COMPLETED**

> **This document is the single source of truth for all backend changes needed to support the new Admin/Driver Dashboard App.**
> Every item has been cross-referenced against the actual C# source code in `src/`.

---

## 1. Domain Layer Modifications (`Taxi.Domain`) — **COMPLETED**

### A. Expand `TripStatus` Enum — **COMPLETED**

**File:** [`TripStatus.cs`](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Domain/Trips/TripStatus.cs)

The current enum is:

```
PendingQuote, Scheduled, PendingDriver, DriverAssigned, InProgress,
Completed, Cancelled, AwaitingPayment, PaymentFailed, Refunded
```

**Add two new states between `DriverAssigned` and `InProgress`:**

```csharp
DriverAssigned,
DriverEnRoute,   // NEW — Driver accepted/was assigned and is heading to pickup
DriverArrived,   // NEW — Driver arrived at pickup location
InProgress,
```

### B. Expand `Trip.cs` State Machine Methods — **COMPLETED**

**File:** [`Trip.cs`](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Domain/Trips/Trip.cs)

Currently `Start()` only allows `Status == DriverAssigned`. Must add intermediate transitions:

1. **Add `DriverEnRoute()` method:**
   - Guard: `Status != TripStatus.DriverAssigned` → error
   - Mutation: `Status = TripStatus.DriverEnRoute`
   - Event: `new DriverEnRoute { TripId, PassengerId, DriverId }`

2. **Add `DriverArrived()` method:**
   - Guard: `Status != TripStatus.DriverEnRoute` → error
   - Mutation: `Status = TripStatus.DriverArrived`
   - Event: `new DriverArrived { TripId, PassengerId, DriverId }`

3. **Update `Start()` guard:**
   - Change from `Status != TripStatus.DriverAssigned` to `Status != TripStatus.DriverArrived`
   - Driver must arrive before starting the trip.

4. **Add timestamp properties:**

   ```csharp
   public DateTimeOffset? AssignedAtUtc { get; private set; }
   public DateTimeOffset? ArrivedAtUtc { get; private set; }
   ```

   - Set `AssignedAtUtc` inside `AssignDriver()`
   - Set `ArrivedAtUtc` inside `DriverArrived()`

### C. Add Domain Events for New States — **COMPLETED**

**Directory:** `Taxi.Domain/Trips/Events/`

Create two new event classes following the existing pattern:

- `DriverEnRoute.cs` — `{ TripId, PassengerId, DriverId }`
- `DriverArrived.cs` — `{ TripId, PassengerId, DriverId }`

### D. Driver KYC Entity — **COMPLETED**

**New File:** `Taxi.Domain/Drivers/DriverDocument.cs`

```csharp
public enum DocumentType { DriversLicense, NationalId, VehicleRegistration, Insurance }
public enum DocumentStatus { Pending, Approved, Rejected }

public sealed class DriverDocument : AuditableEntity
{
    public Guid DriverId { get; private set; }
    public DocumentType Type { get; private set; }
    public string FileUrl { get; private set; }
    public DocumentStatus Status { get; private set; }
    public string? ReviewNotes { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public static Result<DriverDocument> Create(Guid id, Guid driverId, DocumentType type, string fileUrl);
    public Result<Success> Approve(string? notes);
    public Result<Success> Reject(string notes);
}
```

### E. Expand `Driver.cs` with Approval State — **COMPLETED**

**File:** [`Driver.cs`](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Domain/Drivers/Driver.cs)

Add a new `ApprovalStatus` enum and property:

```csharp
public enum DriverApprovalStatus { PendingDocuments, UnderReview, Approved, Suspended }

// Inside Driver class:
public DriverApprovalStatus ApprovalStatus { get; private set; } = DriverApprovalStatus.PendingDocuments;
```

Add methods: `SubmitForReview()`, `Approve()`, `Suspend()`.

**Guard:** `SetStatus(DriverStatus.Online)` must fail if `ApprovalStatus != Approved`.

## 2. Infrastructure Layer Modifications (`Taxi.Infrastructure`) — **COMPLETED**

### A. Extend `AppUser` — **COMPLETED**

**File:** [`AppUser.cs`](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Infrastructure/Identity/AppUser.cs)

Currently an empty class: `public class AppUser : IdentityUser;`

**Add:**

```csharp
public class AppUser : IdentityUser
{
    public bool RequiresPasswordReset { get; set; } = false;
}
```

This requires a new EF Core migration.

### B. Extend `IAppDbContext` and `AppDbContext` — **COMPLETED**

**File:** [`IAppDbContext.cs`](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Application/Common/Interfaces/IAppDbContext.cs)

Add:

```csharp
public DbSet<DriverDocument> DriverDocuments { get; }
```

### C. EF Core Configurations for New Entities — **COMPLETED**

**Directory:** `Taxi.Infrastructure/Data/Configurations/`

- `DriverDocumentConfiguration.cs` — Map `DocumentType` and `DocumentStatus` enums, set `FileUrl` max length, add FK to `Driver`.
- Update `DriverConfiguration.cs` — Add `ApprovalStatus` enum mapping.
- Update `TripConfiguration.cs` — Add `AssignedAtUtc`, `ArrivedAtUtc` columns.

### D. SignalR — Extend `ITripNotifier` and `SignalRTripNotifier` — **COMPLETED**

**File:** [`ITripNotifier.cs`](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Application/Common/Interfaces/ITripNotifier.cs)

Add two new methods:

```csharp
Task NotifyDriverEnRouteAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default);
Task NotifyDriverArrivedAsync(Guid tripId, Guid passengerId, Guid driverId, CancellationToken ct = default);
```

**File:** [`SignalRTripNotifier.cs`](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Infrastructure/RealTime/SignalRTripNotifier.cs)

Implement these two methods, pushing to `Group($"Trip_{tripId}")` with method names `"DriverEnRoute"` and `"DriverArrived"`.

### E. SignalR Notification Contracts — **COMPLETED**

**File:** [`TripNotifications.cs`](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Contracts/Notifications/TripNotifications.cs)

Add:

```csharp
public sealed record DriverEnRouteNotification(Guid TripId, Guid PassengerId, Guid DriverId);
public sealed record DriverArrivedNotification(Guid TripId, Guid PassengerId, Guid DriverId);
```

### F. Update `IdentityService.cs` — Password Reset Support — **COMPLETED**

**File:** [`IdentityService.cs`](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Infrastructure/Identity/IdentityService.cs)

Add methods:

```csharp
Task<Result<Success>> ResetPasswordAsync(string userId, string newPassword);
Task<bool> RequiresPasswordResetAsync(string userId);
Task<Result<Success>> ClearPasswordResetFlagAsync(string userId);
```

### G. Update `TokenProvider.cs` — Include Password Reset Claim — **COMPLETED**

**File:** [`TokenProvider.cs`](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Infrastructure/Identity/TokenProvider.cs)

When generating JWT claims (line 73-82), check `AppUser.RequiresPasswordReset`. If `true`, add:

```csharp
claims.Add(new Claim("requires_password_reset", "true"));
```

### H. Seed Data Update — **COMPLETED**

**File:** [`ApplicationDbContextInitialiser.cs`](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Infrastructure/Data/ApplicationDbContextInitialiser.cs)

When seeding the SuperAdmin (line 127-172):

- Set `RequiresPasswordReset = false` explicitly (SuperAdmin has a known password already).
- When `RegisterAdminCommand` creates new admins, set `RequiresPasswordReset = true` by default.

### I. FCM Push Notification Service — **COMPLETED**

**New Interface:** `Taxi.Application/Common/Interfaces/INotificationService.cs`

```csharp
public interface INotificationService
{
    Task SendPushNotificationAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
}
```

**New Implementation:** `Taxi.Infrastructure/Notifications/FcmNotificationService.cs`

- Uses `FirebaseAdmin` SDK (already initialized in `DependencyInjection.cs` line 34-52)
- Looks up the `FcmToken` from `IAppDbContext.DomainUsers` by `userId`
- Calls `FirebaseMessaging.DefaultInstance.SendAsync(message)`

**Register in DI:** `services.AddScoped<INotificationService, FcmNotificationService>();`

### J. Location Tracking Hub — **COMPLETED**

**New File:** `Taxi.Infrastructure/Hubs/LocationTrackingHub.cs`

A new SignalR Hub at `/hubs/location` (separate from `TripHub`) for high-frequency driver GPS updates.

Methods:

- `UpdateLocation(double lat, double lng)` — Receives driver location, updates `Driver.CurrentLat/CurrentLng/LocationUpdatedAt` via DB, and broadcasts to subscribed admin/passenger connections.

**Register in `Program.cs`:** `app.MapHub<LocationTrackingHub>("/hubs/location");`

---

## 3. Application Layer Modifications (`Taxi.Application`) — **COMPLETED**

### A. Domain Event Handlers for New States — **COMPLETED**

**New Files:** `Taxi.Application/Features/Trips/EventHandlers/`

- `DriverEnRouteEventHandler.cs` — Calls `ITripNotifier.NotifyDriverEnRouteAsync()` AND `INotificationService.SendPushNotificationAsync()` to the passenger.
- `DriverArrivedEventHandler.cs` — Calls `ITripNotifier.NotifyDriverArrivedAsync()` AND `INotificationService.SendPushNotificationAsync()` to the passenger. The FCM payload should trigger an audible alert on the customer's phone.

### B. Driver Trip Lifecycle Commands — **COMPLETED**

All new MediatR commands for the Driver workflow:

| Command               | Endpoint                    | Description                                                             |
| --------------------- | --------------------------- | ----------------------------------------------------------------------- |
| `EnRouteTripCommand`  | `POST /trips/{id}/en-route` | Driver heading to pickup. Transitions `DriverAssigned → DriverEnRoute`. |
| `ArriveTripCommand`   | `POST /trips/{id}/arrive`   | Driver arrived at pickup. Transitions `DriverEnRoute → DriverArrived`.  |
| `StartTripCommand`    | `POST /trips/{id}/start`    | Driver starts ride. Transitions `DriverArrived → InProgress`.           |
| `CompleteTripCommand` | `POST /trips/{id}/complete` | Driver completes ride. Transitions `InProgress → Completed`.            |

Each handler:

1. Loads the Trip from `IAppDbContext.Trips`
2. Validates the caller is the assigned driver (via `IUser.Id`)
3. Calls the corresponding `Trip.XYZ()` domain method
4. Calls `SaveChangesAsync()` — domain events fire automatically

### C. Admin Manual Dispatch Command — **COMPLETED**

**`AssignDriverToTripCommand`** — `POST /trips/{id}/assign`

- **Auth:** Admin only
- **Input:** `TripId`, `DriverId`
- **Process:**
  1. Validate Trip is in `PendingDriver` or `Scheduled` status
  2. Validate Driver exists and is Active (`ApprovalStatus == Approved`)
  3. Call `Trip.AssignDriver(driverId)`
  4. Optionally: Update `Driver.Status = OnTrip`
  5. Save — fires `DriverAssigned` event → SignalR + FCM push

### D. Forced Password Reset Command — **COMPLETED**

**`ForceResetPasswordCommand`** — `POST /auth/force-reset-password`

- **Auth:** Must be authenticated (JWT valid), but accessible even with `requires_password_reset` claim
- **Input:** `NewPassword`
- **Process:**
  1. Get current user ID from `IUser.Id`
  2. Call `IIdentityService.ResetPasswordAsync(userId, newPassword)`
  3. Call `IIdentityService.ClearPasswordResetFlagAsync(userId)`
  4. Generate fresh JWT without the `requires_password_reset` claim
  5. Return new token pair

### E. Driver Management Extensions — **COMPLETED**

| Operation                     | Endpoint                                     | Description                                                              |
| ----------------------------- | -------------------------------------------- | ------------------------------------------------------------------------ |
| `UploadDriverDocumentCommand` | `POST /drivers/{id}/documents`               | Accepts `IFormFile`, stores via `IFileStorage`, creates `DriverDocument` |
| `ReviewDriverDocumentCommand` | `PUT /drivers/{id}/documents/{docId}/review` | Admin approves/rejects a document                                        |
| `ApproveDriverCommand`        | `POST /drivers/{id}/approve`                 | Sets `ApprovalStatus = Approved`                                         |
| `SuspendDriverCommand`        | `POST /drivers/{id}/suspend`                 | Sets `ApprovalStatus = Suspended`                                        |
| `GetDriverDocumentsQuery`     | `GET /drivers/{id}/documents`                | Lists uploaded documents                                                 |
| `UpdateDriverLocationCommand` | `POST /drivers/me/location`                  | Updates `CurrentLat`, `CurrentLng`, `LocationUpdatedAt`                  |
| `SetDriverStatusCommand`      | `POST /drivers/me/status`                    | Sets `Online`/`Offline` (guards `ApprovalStatus == Approved`)            |
| `GetDriverEarningsQuery`      | `GET /drivers/me/earnings`                   | Aggregates trips & fares for the logged-in driver                        |

### G. Admin Queries — **COMPLETED**

| Operation                      | Endpoint                  | Description                                                                  |
| ------------------------------ | ------------------------- | ---------------------------------------------------------------------------- |
| `GetAllTripsQuery`             | `GET /trips`              | Admin paginated list of ALL trips (with filters: status, date range, driver) |
| `GetTripDetailsQuery`          | `GET /trips/{id}/details` | Admin detailed view including passenger info, driver info, payment, stops    |
| `GetAllDriversWithStatusQuery` | `GET /drivers/status`     | Returns drivers with their current online/offline status and location        |
| `GetAuditLogsQuery`            | `GET /audit-logs`         | Paginated audit trail (Admin only)                                           |
| `GetAllUsersQuery`             | `GET /users`              | Admin list of all users (passengers, drivers, admins)                        |

### H. FCM Token Registration — **COMPLETED**

**`UpdateFcmTokenCommand`** — `PUT /users/me/fcm-token`

- Available to all authenticated users
- Calls `User.UpdateFcmToken(token)` (already exists in Domain)
- Used by Customer App, Driver App, and Admin App to register device tokens

---

## 4. API Layer Modifications (`Taxi.Api`) — **COMPLETED**

### A. New Controllers — **COMPLETED**

1. **`DriverDocumentsController`** — Route: `/api/v1/drivers/{id}/documents`, Auth: `[Authorize(Roles = "Admin")]`

### B. Extend Existing Controllers — **COMPLETED**

1. **`TripsController`** — Add:
   - `POST /{id}/en-route` (Driver)
   - `POST /{id}/arrive` (Driver)
   - `POST /{id}/start` (Driver)
   - `POST /{id}/complete` (Driver)
   - `POST /{id}/assign` (Admin)
   - `GET /` (Admin — all trips)
   - `GET /{id}/details` (Admin)

2. **`DriversController`** — Add:
   - `POST /me/status` (Driver — set online/offline)
   - `POST /me/location` (Driver — GPS update)
   - `GET /me/earnings` (Driver)
   - `POST /{id}/approve` (Admin)
   - `POST /{id}/suspend` (Admin)
   - `GET /status` (Admin — all drivers with status)

3. **`AuthController`** — Add:
   - `POST /force-reset-password` (Authenticated, special bypass)

4. **`UsersController`** — Add:
   - `PUT /me/fcm-token` (All roles)
   - `GET /` (Admin — all users list)

### C. Update `Program.cs` — **COMPLETED**

**File:** [`Program.cs`](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Api/Program.cs)

Add: `app.MapHub<LocationTrackingHub>("/hubs/location");` after the existing `TripHub` mapping.

### D. Customer Privacy Enforcement — **COMPLETED**

Ensure that `GetActiveTripQuery` and all trip DTOs returned to Passenger-role users **NEVER** include:

- Driver name
- Driver phone number
- Driver photo

Only return:

- Vehicle Make, Model, Color, License Plate
- Trip Status
- Driver's live GPS coordinates (for map tracking only)

This can be enforced by checking `IUser`'s role claim in the query handler or by having separate DTOs.

---

## 5. Database Migrations Required — **COMPLETED**

After all changes, run:

```bash
dotnet ef migrations add AddDashboardSupport -p src/Taxi.Infrastructure -s src/Taxi.Api
dotnet ef database update -p src/Taxi.Infrastructure -s src/Taxi.Api
```

**New columns/tables:**

- `AspNetUsers.RequiresPasswordReset` (bool, default false)
- `Trips.AssignedAtUtc` (DateTimeOffset?, nullable)
- `Trips.ArrivedAtUtc` (DateTimeOffset?, nullable)
- `Drivers.ApprovalStatus` (int/enum, default 0=PendingDocuments)
- `DriverDocuments` (new table)

---

## 6. Dead Code & Database Cleanup — PromoCode Removal — **COMPLETED**

> **The PromoCode system is not part of this platform. Only the single global discount via `AppConfig` is used.**
> During implementation, ALL promo-code related code and data must be deleted. Do not leave dead code behind.

### A. Delete Domain Files — **COMPLETED**

Delete the following files entirely:

- `Taxi.Domain/Promos/PromoCode.cs`
- `Taxi.Domain/Promos/PromoErrors.cs`
- Any other files inside `Taxi.Domain/Promos/`

### B. Remove from Application Layer

- Remove `DbSet<PromoCode> PromoCodes` from [`IAppDbContext.cs`](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Application/Common/Interfaces/IAppDbContext.cs)
- Delete any existing Commands or Queries under `Taxi.Application/Features/PromoCodes/` (if they exist)

### C. Remove from Infrastructure Layer

- Remove `DbSet<PromoCode> PromoCodes` from `AppDbContext.cs`
- Delete the EF Core configuration file `PromoCodeConfiguration.cs` (if it exists under `Taxi.Infrastructure/Data/Configurations/`)
- Remove any `PromoCode`-related `using` statements from `AppDbContext.cs`

### D. Drop the Database Table

Include a `DropTable` in the same EF Core migration as the other dashboard changes:

```bash
dotnet ef migrations add AddDashboardSupport -p src/Taxi.Infrastructure -s src/Taxi.Api
```

The migration must include:

```csharp
migrationBuilder.DropTable(name: "PromoCodes");
```

> ⚠️ **Important:** If the `PromoCodes` table contains any existing data in production, back it up first before running the migration. After confirming the data is not needed, apply the migration.

### E. Remove from `RequestTripCommandHandler` (if referenced)

Search for any usage of `PromoCode` or `IAppDbContext.PromoCodes` across the `Taxi.Application` project and remove all references. The trip pricing pipeline should only apply the global `TripDiscountPercent` from `AppConfig`.

```bash
# Find all usages to clean up:
grep -r "PromoCode" src/
```

---

## 7. Completed Backend Implementations (Phase Summary)

### A. Dynamic Push Notification Localizer (FCM) — **COMPLETED**

- **Interface & Service:** Modified `INotificationService` and [FcmNotificationService.cs](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Infrastructure/Notifications/FcmNotificationService.cs) to support dynamic language resolution.
- **Mechanism:** Swaps `CultureInfo.CurrentUICulture` dynamically at dispatch time to translate notification keys using the target recipient's `PreferredLanguage` resolved from [SharedResource.\*.json](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Api/Resources/) files via `IStringLocalizerFactory`.
- **Decoupled Handlers:** Refactored event handlers [DriverArrivedEventHandler.cs](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Application/Features/Trips/EventHandlers/DriverArrivedEventHandler.cs) and [DriverEnRouteEventHandler.cs](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Application/Features/Trips/EventHandlers/DriverEnRouteEventHandler.cs) to leverage dynamic keys instead of hardcoded English strings.

### B. User Preferred Language Schema & Endpoint — **COMPLETED**

- **Domain Property:** Added `PreferredLanguage` (default `"en"`) and domain validation method `UpdatePreferredLanguage()` to [User.cs](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Domain/Users/User.cs).
- **API Endpoint:** Implemented `PUT /api/v1/users/me/language` in [UsersController.cs](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Api/Controllers/UsersController.cs) using CQRS `UpdatePreferredLanguageCommand` handlers to process silent background updates from clients.
- **Database Migration:** Generated and applied database schema migration `AddUserPreferredLanguage` mapping the new `PreferredLanguage` column inside [UserConfiguration.cs](file:///c:/Users/Fat7i/myProject/fat7i/TAXI_SERVER/src/Taxi.Infrastructure/Data/Configurations/UserConfiguration.cs).

### C. Unified Vehicle Type & Admin Fallback — **COMPLETED**

- **Vehicle Type Details:** Expanded `TripDto` to include the `VehicleTypeName` property so that clients always receive the selected ride tier.
- **Admin Assignment Fallback:** Modified `GetTripByIdQueryHandler.cs` and `GetAllTripsQueryHandler.cs` to resolve `vehicleModel = vehicleTypeName` if an assigned driver lacks a registered active vehicle (e.g., when an admin assigns a trip to themselves).

### D. Topic Broadcast Support — **COMPLETED**

- **FCM Topic Send:** Added `SendPushNotificationToTopicAsync` to support role-based notifications (`"customers"`, `"drivers"`, `"admins"`) directly from the backend.

---

## 8. Summary of Backend Status

| Category                 | Status        | Details                                                               |
| ------------------------ | ------------- | --------------------------------------------------------------------- |
| New Domain Entities      | **COMPLETED** | `DriverDocument`                                                      |
| Modified Domain Entities | **COMPLETED** | `Trip`, `Driver`, `TripStatus`, `User`                                |
| New Domain Events        | **COMPLETED** | `DriverEnRoute`, `DriverArrived`                                      |
| New API Endpoints        | **COMPLETED** | Location Tracking, Status Updates, Preferred Language                 |
| FCM Localization         | **COMPLETED** | Swaps culture context dynamically via `IStringLocalizerFactory`       |
| Database Migrations      | **COMPLETED** | Applied `AddDashboardSupport` and `AddUserPreferredLanguage`          |
| **Deleted Code**         | **COMPLETED** | Purged all PromoCode domain logic, DbSets, configurations, and tables |
