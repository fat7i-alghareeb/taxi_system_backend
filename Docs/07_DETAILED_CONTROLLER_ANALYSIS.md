# 07. Detailed Controller & Endpoints Analysis

This document maps every single Controller, its exact REST routes, Authorization requirements, and the MediatR command/query it delegates to.

---

## 1. `AppConfigController`
**Route:** `/api/v1/app-config`
- **GET `/client`**
  - **Auth:** `[AllowAnonymous]`
  - **Action:** `GetClientConfigQuery`
  - **Purpose:** Public configuration payload for mobile app boot. Returns whether Stripe is enabled and the Stripe Publishable Key.
- **GET `/trip-discount`**
  - **Auth:** `[Authorize(Roles = "Admin")]`
  - **Action:** `GetTripDiscountQuery`
  - **Purpose:** Admin dashboard view of the current global discount percentage.
- **PUT `/trip-discount`**
  - **Auth:** `[Authorize(Roles = "Admin")]`
  - **Action:** `UpdateTripDiscountCommand`
  - **Purpose:** Admin dashboard command to alter the global discount.

---

## 2. `AuthController`
**Route:** `/api/v1/auth`
- **POST `/login`**
  - **Auth:** `[AllowAnonymous]`
  - **Action:** `LoginCommand(Phone, FirebaseIdToken, FcmToken)`
  - **Purpose:** Main entry point for mobile apps. Validates SMS OTP via Firebase and generates system JWTs. Silently registers new users.

---

## 3. `IdentityController`
**Route:** `/api/v1/identity`
- **POST `/tokens`**
  - **Auth:** `[AllowAnonymous]`
  - **Action:** `GenerateTokenQuery(Email, Password)`
  - **Purpose:** Classic username/password login (primarily for Backend Admins, as mobile users use phone auth).
- **POST `/tokens/refresh`**
  - **Auth:** `[AllowAnonymous]`
  - **Action:** `RefreshTokenQuery(ExpiredAccessToken, RefreshToken)`
  - **Purpose:** Exchanging an expired JWT and valid Refresh Token for a new pair.
- **GET `/current-user/claims`**
  - **Auth:** `[Authorize]`
  - **Action:** `GetUserByIdQuery`
  - **Purpose:** Diagnostics endpoint to read the decoded claims attached to the JWT.
- **POST `/admins`**
  - **Auth:** `[Authorize(Roles = "Admin")]`
  - **Action:** `RegisterAdminCommand`
  - **Purpose:** Allows existing Admins to bootstrap new Admin accounts.

---

## 4. `DriversController`
**Route:** `/api/v1/drivers`
**Auth:** Entire controller is `[Authorize(Roles = "Admin")]`
- **GET `/`** â†’ `GetDriversQuery` (Returns all drivers)
- **GET `/{id}`** â†’ `GetDriverByIdQuery`
- **POST `/`** â†’ `CreateDriverCommand`
- **PUT `/{id}`** â†’ `UpdateDriverCommand` (Updates License)
- **DELETE `/{id}`** â†’ `DeleteDriverCommand` (Soft delete)
- **POST `/{id}/vehicles`** â†’ `AssignVehicleCommand` (Assigns a physical car to a driver profile)

---

## 5. `VehiclesController`
**Route:** `/api/v1/vehicles`
**Auth:** Entire controller is `[Authorize(Roles = "Admin")]`
- **GET `/`** â†’ `GetVehiclesQuery`
- **GET `/{id}`** â†’ `GetVehicleByIdQuery`
- **POST `/`** â†’ `CreateVehicleCommand` (Registers Make, Model, License Plate)
- **PUT `/{id}`** â†’ `UpdateVehicleCommand` (Updates color, plate, active status)
- **DELETE `/{id}`** â†’ `RemoveVehicleCommand`

---

## 6. `VehicleTypesController`
**Route:** `/api/v1/vehicle-types`
- **GET `/`** 
  - **Auth:** `[AllowAnonymous]` / Implicitly public for users.
  - **Action:** `GetVehicleCatalogQuery`
  - **Note:** Marked with `[OutputCache(Duration = 60)]`. Highly optimized.
- **GET `/{id}`** â†’ `GetVehicleTypeByIdQuery`
- **GET `/code/{code}`** â†’ `GetVehicleTypeByCodeQuery` (e.g., "PRM")
- **POST `/`** 
  - **Auth:** `[Authorize(Roles = "Admin")]`
  - **Action:** `CreateVehicleTypeCommand`
- **PUT `/{id}`** 
  - **Auth:** `[Authorize(Roles = "Admin")]`
  - **Action:** `UpdateVehicleTypeCommand` (Updates pricing: RatePerKm, RatePerMin)
- **DELETE `/{id}`** 
  - **Auth:** `[Authorize(Roles = "Admin")]`
  - **Action:** `RemoveVehicleTypeCommand`

---

## 7. `MapsController`
**Route:** `/api/v1/maps`
**Auth:** Entire controller is `[Authorize]`
- **POST `/directions`**
  - **Action:** `GetDirectionsQuery(List<CoordinateDto> Stops)`
  - **Purpose:** Takes N stops, returns distance, duration, and Encoded Polyline for route drawing.
- **GET `/reverse-geocode`**
  - **Action:** `ReverseGeocodeQuery(Latitude, Longitude)`
  - **Purpose:** Converts GPS pins into human readable addresses (e.g. "Main Street 123").
- **GET `/places`**
  - **Action:** `SearchPlacesQuery(Query, Latitude, Longitude)`
  - **Purpose:** Powers autocomplete location search fields.

---

## 8. `TripsController`
**Route:** `/api/v1/trips`
**Auth:** Entire controller is `[Authorize]`
- **POST `/quotes`**
  - **Action:** `GetPricingQuotesCommand(Stops)`
  - **Purpose:** Evaluates pricing logic against vehicle types. Returns quotes.
- **POST `/request`**
  - **Action:** `RequestTripCommand(QuoteId, Stops, ScheduledAt)`
  - **Purpose:** Consumes a quote. Generates Stripe PaymentIntent. Initializes Trip state machine.
- **POST `/{id}/cancel`**
  - **Action:** `CancelTripCommand(TripId)`
  - **Purpose:** Passenger cancels trip. Issues Stripe cancellation or refund automatically.
- **GET `/me/active`**
  - **Action:** `GetActiveTripQuery()`
  - **Purpose:** Returns the single trip currently `InProgress` or `Pending` for the user. Used to restore state if app restarts.
- **GET `/me/history`**
  - **Action:** `GetPassengerTripsQuery(Page, PageSize)`
  - **Purpose:** Paginated historical rides list.
- **GET `/{id}`**
  - **Action:** `GetTripByIdQuery(TripId)`
  - **Purpose:** Detailed receipt view of a past trip.

---

## 9. `UsersController`
**Route:** `/api/v1/users`
**Auth:** Entire controller is `[Authorize]`
- **GET `/me`**
  - **Action:** `GetCurrentUserQuery`
  - **Purpose:** Returns user profile.
- **POST `/me`**
  - **Action:** `UpdateUserProfileCommand([FromForm] Name, Photo)`
  - **Purpose:** Form-data endpoint to upload a profile picture and name.

---

## 10. `WebhooksController`
**Route:** `/api/webhooks`
- **POST `/stripe`**
  - **Auth:** `[AllowAnonymous]` (Protected by Cryptographic Signature, not JWT).
  - **Action:** `HandleStripeWebhookCommand`
  - **Purpose:** Consumes `payment_intent.succeeded`, `payment_intent.failed`, and `charge.refunded` events directly from Stripe servers to progress the Trip state machine without client interaction.
