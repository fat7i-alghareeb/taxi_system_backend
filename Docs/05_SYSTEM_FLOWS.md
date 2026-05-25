# 05. System Flows & Orchestration Exhaustive Analysis

This final document maps how the different layers (Domain, Application, Infrastructure, API) interact to fulfill the core business requirements of the Taxi platform.

---

## 1. The Trilingual Architecture (Localization Flow)

The system supports 9 languages, but the backend doesn't use massive JOIN tables.
1. **Client Request**: The mobile app sends `Accept-Language: ar` in the HTTP Header.
2. **API Layer**: `RequestLocalizationMiddleware` intercepts this and sets `CultureInfo`. `LanguageContext` (scoped service) captures the exact language code (e.g., `ar`).
3. **Application Layer**: A MediatR handler (e.g., `GetVehicleCatalogQueryHandler`) executes. It reads `LanguageContext.Language`.
4. **Infrastructure (EF Core)**: The LINQ query projects the JSONB column directly:
   ```csharp
   lang == Languages.Ar ? t.Name.Ar : t.Name.En
   ```
5. **Database**: PostgreSQL evaluates `Name->>'Ar'` directly inside the SQL engine. The database only returns a single string over the network, minimizing bandwidth.

---

## 2. Authentication & Session Flow

The app relies on Firebase for SMS verification to avoid paying for custom SMS gateways, but uses its own JWTs for granular permissions.
1. **Mobile App**: Prompts user for Phone Number. Receives SMS. Sends code to Google Firebase. Receives `FirebaseIdToken`.
2. **API (`AuthController`)**: App POSTs the Firebase token to `/auth/login`.
3. **Application (`LoginCommandHandler`)**: 
   - Validates Firebase Token via `IFirebaseAuthService`. Extracts verified phone number.
   - Searches `DomainUsers` by phone. If it doesn't exist, it creates a new `User` (Role = Passenger) and syncs to ASP.NET Identity.
4. **Infrastructure (`TokenProvider`)**: 
   - Generates a short-lived `AccessToken` (JWT).
   - Generates a 32-byte cryptographically secure `RefreshToken`, sets it to expire in 30 days, and saves it to the DB.
5. **Ongoing**: When the Access Token expires, the app calls `/identity/tokens/refresh` to get a new pair without requiring another SMS.

---

## 3. The Core Trip & Payment Flow

This is the most complex orchestration in the system, involving Domain State Machines, Google Maps, Stripe, and SignalR.

### Phase A: Quoting
1. **Passenger**: Drops two pins on the map (Pickup and Dropoff). Calls `/trips/quotes`.
2. **Google Maps (`IDirectionsService`)**: Calculates accurate driving distance (e.g., 5.2 km) and duration (12 mins).
3. **Pricing Engine**: Multiplies distance/duration by active `VehicleType` rates (Standard, Premium). Applies active `TripDiscountPercent`.
4. **Database**: Quotes are saved to the `PricingQuotes` table and are valid for exactly 15 minutes.

### Phase B: Requesting & Stripe Intent
1. **Passenger**: Selects "Premium" and taps "Request". Calls `/trips/request` with `QuoteId`.
2. **Domain (`Trip.Request`)**: The `Trip` aggregate is created. State = `AwaitingPayment`. The Quote is marked as `Used`.
3. **Stripe Integration**: `IStripePaymentService.CreatePaymentIntentAsync` is called for the Exact `FinalFare`.
4. **Response**: The API returns the Trip DTO containing the Stripe `ClientSecret`. The mobile app natively renders the Stripe PaymentSheet.

### Phase C: Webhook Execution (The Async Handoff)
1. **Passenger**: Enters Credit Card in the mobile app. Stripe processes it.
2. **Stripe Server**: Sends an asynchronous webhook to `POST /webhooks/stripe`.
3. **API (`WebhooksController`)**: Verifies the Stripe Cryptographic Signature.
4. **MediatR (`HandleStripeWebhookCommandHandler`)**: 
   - Reads `payment_intent.succeeded`.
   - Finds the matching `Payment` in the DB. Marks it `Completed`.
   - Calls `Trip.ConfirmPayment()`. State â†’ `PendingDriver`.
   - Triggers `TripDispatchHelper` to assign an active driver.
5. **Domain Events**: `Trip.ConfirmPayment()` generates a `PaymentConfirmed` event. `AssignDriver()` generates a `DriverAssigned` event.
6. **SignalR (`ITripNotifier`)**: The `DriverAssignedEventHandler` catches the event and immediately pushes a WebSocket message to `Group("Trip_{TripId}")`.
7. **Mobile App**: The passenger's screen instantly updates from a "Loading Payment..." spinner to showing the Driver's car on the map, all without polling.

---

## 4. The Cancellation & Refund Flow

1. **Passenger**: Taps "Cancel Trip". Calls `POST /trips/{id}/cancel`.
2. **Validation**: The Application layer ensures the trip hasn't started yet.
3. **Domain**: Calls `Trip.Cancel()`. State â†’ `Cancelled`.
4. **Stripe Logic**:
   - If the Payment was `Pending` (user closed the PaymentSheet), it calls Stripe to `CancelPaymentIntentAsync` so the hold drops immediately.
   - If the Payment was `Completed` (driver was assigned but passenger cancelled), it calls Stripe to `CreateRefundAsync`.
5. **Webhook Loopback**: A few seconds later, Stripe sends `charge.refunded` to the webhook endpoint. The webhook handler marks the Domain `Payment` as `Refunded` and updates the `Trip` state to `Refunded`. It then fires a SignalR event to notify the UI.

---

## 5. Audit Logging Architecture

1. An Admin modifies a `VehicleType` pricing model.
2. The `UpdateVehicleTypeCommandHandler` modifies the entity and calls `_context.SaveChangesAsync()`.
3. **`AuditLogInterceptor`** pauses the save.
4. It iterates over `ChangeTracker.Entries()`.
5. It reads the `IUser.Id` (from the JWT via `HttpContext`).
6. It serializes the Old Values (e.g., `RatePerKm = 1.00`) and the New Values (e.g., `RatePerKm = 1.25`) into JSON.
7. It creates an `AuditLog` row containing the Action (`Update`), Table (`VehicleType`), User ID, and the diff JSON.
8. The database transaction commits both the pricing change and the audit log in one atomic operation.
