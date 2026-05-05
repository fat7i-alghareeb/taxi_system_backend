# 🚕 Fat7i Taxi Backend - Implementation Roadmap

---

## 📦 PHASE 0: SYSTEM FOUNDATION [COMPLETED]

> **STATUS:** LOCKED ✅
> **OBJECTIVE**: Orchestrate the environment and establish core trilingual infrastructure.
>
> **ACCOMPLISHMENTS**:
>
> - **Environment**: Database isolation for `Dev` and `Prod` via Docker.
> - **Clean Slate**: Stale `Cars` entities and database volumes fully wiped.
> - **Core**: `LocalizedText` value object implemented (En/Ar/Nl) with JSONB support.
> - **Persistence**: `AppDbContext` and Interceptors configured for net10.

---

## 📦 PHASE 1: AUTHENTICATION & IDENTITY [COMPLETED]

> **STATUS:** LOCKED ✅
> **OBJECTIVE**: Phone + OTP verification flow with MechanicShop architectural parity.
>
> **ACCOMPLISHMENTS**:
>
> - **Domain**: `User` aggregate (Trilingual Name, Phone, Role) with Domain validation.
> - **Application**: Vertical slices (`SendOtp`, `VerifyOtp`) using 3-file pattern (Command/Handler/Validator).
> - **Infrastructure**: `OtpService` (In-memory), `ConsoleSmsProvider`, `IdentityService` updated.
> - **API**: v1 `AuthController` with RFC 7807 problem details and Swagger documentation.
> - **Localization**: `SharedResource` JSON files synchronized with all Auth error keys.

---

## 🏎️ Phase 2: Vehicles & Pricing [IN PROGRESS]

_Goal: Centralize pricing authority on the server and manage vehicle catalog._

### Domain Layer

- [ ] **VehicleType Entity**: Store localized `Name`, `Description`, and pricing fields.
- [ ] **Vehicle Entity**: Independent aggregate for physical car details (LicensePlate, Make, Model).
- [ ] **Driver-Vehicle Link**: Logic to associate a driver with an `ActiveVehicle`.

### Application Layer

- [ ] **PricingService**: Implement core fare calculation: `MAX((D*rate_km + T*rate_min), min_fare)`.
- [ ] **Vehicle Management**: Commands to create and assign vehicles to drivers.

### Infrastructure Layer

- [ ] **GoogleMapsService**: Implement `IDirectionsService` for real-world distance/duration.
- [ ] **Configurations**: JSONB mappings for `VehicleType` and `Vehicle`.

---

## 📍 Phase 3: Trip Lifecycle & Quotes

_Goal: Implement the Trip FSM and Quote locking mechanism._

### Domain Layer

- [ ] **Trip Aggregate**: Implement the FSM: `PENDING_QUOTE` → `PENDING_DRIVER` → `TRIP_IN_PROGRESS` → `COMPLETED`.
- [ ] **Stop Value Object**: A stop with `Coordinate`, `Address`, and `Sequence`.
- [ ] **Quote Entity**: A locked fare with a 5-minute `ExpiresAtUtc` timestamp.

---

## 📡 Phase 4: Driver & Real-Time Sync

_Goal: Manage driver state and push updates to passengers._

---

## 💳 Phase 5: Payments & Promos

_Goal: Finalize the trip and process the transaction._

---

## 📊 Phase 6: Logging & Monitoring

_Goal: Infrastructure for transparency and auditability._

### Infrastructure Layer

- [ ] **AuditLog Service**: Interceptor-based logging of all entity changes and admin actions.
- [ ] **Notification Registry**: Store history of all sent Push and SMS notifications.
