# 09. Dashboard App — Features, Use Cases & App Flow (Admin & Driver)

> **This is the product specification for the Fat7i Dashboard Application.**
> It defines WHAT the app does, WHO uses it, and HOW every feature works — without prescribing any specific technology or framework.
>
> Every requirement is derived from exhaustive analysis of the actual backend source code in `TAXI_SERVER/src/`.
>
> **Decisions Locked In:**
>
> - ✅ `DriverEnRoute` + `DriverArrived` states added to Trip state machine
> - ✅ Driver auth = Firebase Phone SMS (same as passengers)
> - ✅ Manual dispatch ONLY — Admin assigns drivers. Admin can also act as driver.
> - ✅ Strict privacy — Customer sees vehicle type/trip status only, never driver identity.
> - ✅ Full KYC pipeline from day one
> - ❌ PromoCode system — DOES NOT EXIST. Only a single global discount via AppConfig.
> - ❌ Ratings skipped for V1

---

## 1. Roles & Access Model

The app serves **two roles** within a single application:

| Role       | Description                                                                       | Access Level                                                |
| ---------- | --------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| **Admin**  | System operator who manages drivers, vehicles, trips, pricing, and configurations | Full access to everything, including all Driver features    |
| **Driver** | Receives trip assignments and executes rides                                      | Access only to their own trips, location, earnings, and KYC |

- A single user may hold **both** Admin and Driver roles simultaneously.
- When a user has both roles, they see the Admin interface by default and can switch to "Driver Mode" to execute trips themselves.

---

## 2. Authentication & Security

### 2.1 Login Flow

- Both Admin and Driver use **Firebase Phone Authentication** (SMS OTP), identical to the Customer App.
- Flow: Enter phone → Receive SMS → Enter OTP → Firebase returns ID token → App sends token to backend → Backend returns JWT + Refresh Token.
- The backend endpoint is `POST /api/v1/auth/login` with `{ phone, firebaseIdToken, fcmToken }`.

### 2.2 Role Detection

- After login, the app reads the `role` claim from the JWT.
- Admin → admin interface.
- Driver → driver interface.
- Both roles → admin interface with Driver Mode accessible.

### 2.3 Forced Password Reset

When a SuperAdmin provisions a new Admin account (via `POST /identity/admins`), the account is created with `RequiresPasswordReset = true`.

1. On the new Admin's first login, the JWT includes a `requires_password_reset: true` claim.
2. The app detects this flag and **blocks access to all screens** except the password reset screen.
3. The user enters a new password + confirmation.
4. The app calls `POST /api/v1/auth/force-reset-password { newPassword }`.
5. The backend clears the flag and returns a fresh JWT without the claim.
6. The app stores the new JWT and proceeds to the dashboard.

### 2.4 Session Management

- Short-lived JWT access tokens (configurable, typically minutes).
- Long-lived refresh tokens (configurable, typically 7–30 days).
- Automatic transparent token refresh on 401 responses — the user never sees a re-login prompt.
- On refresh failure or token revocation → immediate logout and redirect to login.

### 2.5 Navigation Guard Rules

All navigation must be gated by a guard that evaluates linearly:

1. **Not authenticated** → redirect to Login
2. **Authenticated + `requires_password_reset`** → redirect to Force Password Reset (block everything else)
3. **Authenticated + Admin role** → allow Admin routes
4. **Authenticated + Driver role** → allow Driver routes
5. **Authenticated + both roles** → allow all routes

---

## 3. Admin Features

### 3.1 Dashboard Home

The main landing screen providing a real-time operational overview.

**Data Displayed:**

- Total Active Trips (live, updated via SignalR)
- Total Online Drivers (live)
- Today's Revenue (aggregated completed trips)
- Pending KYC Reviews count
- Recent Activity Feed: last 5–10 trip events (assigned, completed, cancelled) streaming in real-time

**Actions:**

- Tap any stat card → navigate to the relevant management screen
- Compact live map preview showing online driver positions → tap to open full-screen tracking

---

### 3.2 Live Tracking — God Mode Map

**Purpose:** Full-screen map showing ALL online drivers in real-time.

**Data Sources:**

- SignalR `LocationTrackingHub` (`/hubs/location`) — receives `DriverLocationUpdated(driverId, lat, lng)` every ~10 seconds
- Each driver rendered as a car marker with smooth position interpolation

**Map Behavior:**

- Driver markers color-coded by status:
  - 🟢 Green = Online/Idle (available for trips)
  - 🔴 Red = On Trip (currently executing a ride)
- Tap a driver marker → bottom panel shows: Driver Name, Vehicle Type, Current Trip (if any)
- Pending trips shown as pickup pin markers on the map
- Tap a pending trip → option to manually dispatch a driver to it

---

### 3.3 Trip Management

**Use Cases:**

#### UC-3.3.1: View All Trips

- **Endpoint:** `GET /api/v1/trips`
- Paginated list of ALL trips in the system
- **Filters:** Status (PendingDriver, DriverAssigned, DriverEnRoute, DriverArrived, InProgress, Completed, Cancelled), date range, driver, vehicle type
- **Columns:** Reference Code, Passenger Phone, Status (color-coded), Vehicle Type, Fare, Created Date

#### UC-3.3.2: View Trip Details

- **Endpoint:** `GET /api/v1/trips/{id}/details`
- Trip timeline visualization showing each state transition with timestamps:
  `Requested → Payment → Assigned → En Route → Arrived → Started → Completed`
- Passenger info: Phone (partially masked for privacy)
- Driver info: Name, License, Vehicle Type
- Payment info: Amount, Currency, Payment Status, Stripe Payment Intent ID
- Stops: Pickup + Dropoff + Waypoints displayed on a mini-map

**Implementation note:** The dashboard Flutter app now has a dedicated Trip Management route. It loads the admin trip list from `GET /api/v1/trips/admin` and selected trip details from `GET /api/v1/trips/{id}/details`, showing passenger, driver, vehicle type, fare, pickup/dropoff labels, and lifecycle timestamps. Date-range filtering and mini-map route preview remain later enhancements.

#### UC-3.3.3: Manual Dispatch (Assign Driver to Trip)

- **Endpoint:** `POST /api/v1/trips/{id}/assign { driverId }`
- **Precondition:** Trip must be in `PendingDriver` or `Scheduled` status
- **Flow:**
  1. Admin opens a PendingDriver trip
  2. Taps "Assign Driver"
  3. A list of available online drivers appears (showing name, vehicle type, distance to pickup if possible)
  4. Admin selects a driver
  5. Backend calls `Trip.AssignDriver(driverId)` → fires `DriverAssigned` domain event
  6. SignalR pushes update to Customer App instantly
  7. FCM push sent to the assigned driver

#### UC-3.3.4: Self-Assignment (Admin as Driver)

- **Precondition:** Admin must also have the Driver role and a registered Driver profile
- **Flow:**
  1. In the "Assign Driver" list, the Admin's own driver profile appears
  2. Admin selects themselves
  3. Same backend flow as UC-3.3.3 but with the Admin's own `driverId`
  4. Admin can then switch to Driver Mode to execute the trip

---

### 3.4 Driver Management

**Use Cases:**

#### UC-3.4.1: View All Drivers

- **Endpoint:** `GET /api/v1/drivers`
- List showing: Name, Phone, Online/Offline status, Approval Status (Pending/Approved/Suspended), Vehicle Type, Total Completed Trips
- **Filters:** Approval Status, Online Status

#### UC-3.4.2: View Driver Detail

- **Endpoint:** `GET /api/v1/drivers/{id}`
- Profile: Name, Phone, License Number, Acceptance Rate, Completion Rate
- Vehicle Type: Currently assigned driver vehicle type + option to change
- KYC Documents: List of uploaded documents with preview thumbnails
  - Each document: Type (License/ID/Registration/Insurance), Status (Pending/Approved/Rejected), Upload Date
  - Admin can view full image and approve/reject with notes
- Actions: Approve / Suspend / Edit

#### UC-3.4.3: Create Driver

- **Endpoints:** `POST /api/v1/drivers` (creates Driver + User + Identity account)
- **Input:** Phone, Name (9 languages), License Number
- **Result:** New driver created with `ApprovalStatus = PendingDocuments`. The driver must log in via phone, upload KYC documents, and await Admin approval before going online.

#### UC-3.4.4: KYC Document Review

- **Endpoint:** `PUT /api/v1/drivers/{id}/documents/{docId}/review`
- Admin views each uploaded document
- For each document: **Approve** or **Reject** (with mandatory rejection notes)
- When all required documents are approved → driver can be approved

#### UC-3.4.5: Approve / Suspend Driver

- **Approve:** `POST /api/v1/drivers/{id}/approve` → sets `ApprovalStatus = Approved`
- **Suspend:** `POST /api/v1/drivers/{id}/suspend` → sets `ApprovalStatus = Suspended`
- A suspended driver cannot go Online. An unapproved driver cannot go Online.

#### UC-3.4.6: Assign Vehicle Type to Driver

- **Endpoint:** `POST /api/v1/drivers/{id}/vehicle-type`
- Links one vehicle type to a driver profile
- A driver must have an assigned vehicle type to go Online

**Implementation note:** The dashboard Flutter app now includes an Admin Operations route that aggregates `/api/v1/drivers`, `/api/v1/users`, `/api/v1/vehicle-types`, `/api/v1/audit-logs`, `/api/v1/app-config/trip-discount`, `/api/v1/app-config/currency`, and `/api/v1/app-config/client`. The first pass supports driver suspension, driver vehicle-type assignment, vehicle-type active toggling/removal, global trip discount update, currency update, user visibility, and full audit viewing.

---

### 3.5 Vehicle Management

#### UC-3.5.1: Vehicle Type Management (Pricing Engine)

- **List:** `GET /api/v1/vehicle-types` — shows all types (Standard, XL Van, Wheelchair) with:
  - Localized name (9 languages)
  - Rate/km, Rate/min, Minimum Fare
  - Currency, Passenger Capacity, Sort Order
  - Active/Inactive toggle
- **Create:** `POST /api/v1/vehicle-types` — full form with 9-language names + pricing fields
- **Edit Pricing:** `PUT /api/v1/vehicle-types/{id}` — update Rate/km, Rate/min, Minimum Fare, Active status, Sort Order
- **Delete:** `DELETE /api/v1/vehicle-types/{id}`

#### UC-3.5.2: Physical Vehicle Management

- Removed from the current product model. The system manages vehicle types only; no car, plate, make, model, or physical vehicle inventory is required for driver assignment. The backend includes a schema cleanup migration that drops the legacy `Vehicles` table and `DomainUsers.ActiveVehicleId`.

---

### 3.6 System Configuration

#### UC-3.6.1: Global Trip Discount

- **View:** `GET /api/v1/app-config/trip-discount` — shows current percentage (e.g., 5%)
- **Update:** `PUT /api/v1/app-config/trip-discount` — slider or input (0–100%)
- Changes apply immediately to all future pricing quotes

#### UC-3.6.2: System Info (Read-Only)

- **View:** `GET /api/v1/app-config/client`
- Displays: Stripe enabled/disabled, Stripe Publishable Key
- General system health metrics

---

### 3.7 User Management

#### UC-3.8.1: View All Users

- **Endpoint:** `GET /api/v1/users`
- Paginated list: Name, Phone, Role (Passenger/Driver/Admin), Active status, Join date
- Filter by role

#### UC-3.8.2: Create New Admin

- **Endpoint:** `POST /api/v1/identity/admins`
- **Fields:** Phone, Email, Password, Name (9 languages)
- New admin is created with `RequiresPasswordReset = true`

---

### 3.8 Audit Log Viewer

#### UC-3.8.1: View Audit Trail

- **Endpoint:** `GET /api/v1/audit-logs`
- Paginated table: Timestamp, User (who made the change), Action (Created/Updated/Deleted), Entity Name (Trip, VehicleType, etc.), Entity ID
- **Filters:** Date range, User, Entity type
- Each row expandable to show the Old Value → New Value JSON diff

---

## 4. Driver Features

### 4.1 Driver Home

A simple, high-contrast interface optimized for in-vehicle use.

**Elements:**

- **Large Online/Offline Toggle** — the primary interaction point
- **Current Trip Card** — if a trip is assigned, show details prominently
- **Today's Quick Stats** — trips completed today, earnings today

**Online/Offline Logic:**

- Going Online: `POST /api/v1/drivers/me/status { status: "Online" }`
  - **Guards:** Only succeeds if `ApprovalStatus == Approved` AND a vehicle type is assigned
  - Starts continuous GPS location tracking
- Going Offline: `POST /api/v1/drivers/me/status { status: "Offline" }`
  - Stops GPS tracking
  - Driver removed from available pool

---

### 4.2 GPS Location Tracking (While Online)

When the driver is Online, the app must continuously track and report their location:

- **Frequency:** Every 10 seconds
- **Method:** SignalR `LocationTrackingHub.UpdateLocation(lat, lng)` (preferred for low latency) or HTTP `POST /api/v1/drivers/me/location { lat, lng }` as fallback
- **Server Side:** Updates `Driver.CurrentLat`, `Driver.CurrentLng`, `Driver.LocationUpdatedAt`
- **Broadcast:** Server pushes the coordinates to:
  - Admin's God-Mode Map (all online drivers)
  - The assigned Passenger's Customer App (during active trip)
- **Background Tracking:** Must continue even when the app is minimized — requires persistent background location permission

---

### 4.3 Trip Assignment Reception

When a trip is manually assigned to a driver by the Admin:

**Real-Time Channel (SignalR):**

- Driver is connected to `TripHub` and subscribed to their VehicleType group
- When `DriverAssigned` event fires with their `driverId`, they receive it instantly

**Background Channel (FCM):**

- If the app is backgrounded or killed, the server sends an FCM push notification
- Tapping the notification opens the app directly to the Trip Execution screen

**UI Behavior:**

- A full-screen alert/overlay appears showing:
  - Trip Reference Code
  - Pickup Address (from first TripStop `Label`, falling back to coordinates)
  - Dropoff Address (from last TripStop `Label`, falling back to coordinates)
  - Vehicle Type
  - Fare Amount
- **No reject button** — Admin has manually assigned, so the driver must execute
- "Accept" / "Start Navigation" proceeds to the trip execution flow

---

### 4.3.1 Admin Operations Overview Contract

- Dashboard overview reads live admin data from `/api/v1/drivers`, `/api/v1/trips/admin`, and `/api/v1/audit-logs`.
- Dashboard overview reads the vehicle-type catalog from `/api/v1/vehicle-types` so the admin landing screen can show driver capability and pricing context without physical vehicle inventory.
- The dashboard live map opens a dedicated full-screen route seeded by `GET /api/v1/drivers/status`; after load, it listens to `DriverLocationUpdated` realtime events and updates existing markers without refetching the whole dashboard overview. Pending dispatch trips are plotted as pickup pins from the first `TripStop` coordinate in `/api/v1/trips/admin`, and tapping a pickup opens the existing driver assignment sheet with available drivers ranked by haversine distance to the pickup.
- `DriverDto` includes `ApprovalStatus` so dashboard KYC/review counts do not infer approval state from online/offline driver status.
- Audit activity is exposed through `GET /api/v1/audit-logs?page=1&pageSize=15` and is restricted to Admin users.
- The dashboard app aggregates counts client-side for the first operations pass while keeping DTOs stable and reusable for later dedicated reporting endpoints.
- Pending driver rows open a KYC review sheet backed by `GET /api/v1/drivers/{id}/documents`, `PUT /api/v1/drivers/{id}/documents/{docId}/review`, and `POST /api/v1/drivers/{id}/approve`.
- Pending dispatch rows use `POST /api/v1/trips/{id}/assign` with an online, approved driver whose `vehicleTypeId` matches the requested trip vehicle type.

---

### 4.4 Trip Execution Lifecycle

The driver progresses through the trip in 4 sequential steps via large, touch-safe action buttons:

#### Step 1: En Route to Pickup

- **Trigger:** Automatically when trip is assigned (or driver taps "I'm on my way")
- **API Call:** `POST /api/v1/trips/{id}/en-route`
- **State Transition:** `DriverAssigned → DriverEnRoute`
- **UI:**
  - Map shows route from driver's location to pickup point
  - "Navigate" action opens the device's default navigation app (Google Maps / Waze) with pickup coordinates
  - ETA displayed
- **Customer Side:** Customer App receives SignalR event + FCM push: "A vehicle is on the way"

#### Step 2: Arrived at Pickup

- **Trigger:** Driver taps large **"I Have Arrived"** button
- **API Call:** `POST /api/v1/trips/{id}/arrive`
- **State Transition:** `DriverEnRoute → DriverArrived`
- **Customer Side:** Customer receives prominent notification: "Your ride is outside" with vehicle type and trip status only

#### Step 3: Start Trip

- **Trigger:** Passenger enters the vehicle. Driver taps **"Start Trip"** button.
- **API Call:** `POST /api/v1/trips/{id}/start`
- **State Transition:** `DriverArrived → InProgress`
- **UI:** Map switches to show route from current location to dropoff destination. Navigation available.

#### Step 4: Complete Trip

- **Trigger:** Driver arrives at destination. Driver taps **"Complete Trip"** button.
- **API Call:** `POST /api/v1/trips/{id}/complete`
- **State Transition:** `InProgress → Completed`
- **UI:** Trip summary displayed: Fare, Duration, Distance
- Driver returns to home screen, ready for next assignment

---

### 4.5 Trip History & Earnings

**Endpoint:** `GET /api/v1/drivers/me/earnings`

**Features:**

- **Time Periods:** Daily / Weekly / Monthly views
- **Per-Trip Breakdown:** Reference Code, Fare Amount, Pickup → Dropoff addresses, Duration, Date
- **Summary Totals:** Total trips, total fare collected, platform commission (if applicable), net earnings

**Implementation note:** Driver Home now fetches `GET /api/v1/drivers/me/earnings` through the driver feature repository/facade and binds the idle map panel to real total trips and earnings instead of static placeholder values.

---

### 4.6 Driver KYC (Know Your Customer) Flow

**Purpose:** Drivers must upload identity and vehicle documents before they can go online. Admin reviews and approves them.

**Driver Experience:**

| Driver's Approval Status | What They See                                       |
| ------------------------ | --------------------------------------------------- |
| `PendingDocuments`       | Document upload screen with required slots          |
| `UnderReview`            | "Your documents are under review" waiting screen    |
| `Approved`               | Full access to Driver Home + can go Online          |
| `Suspended`              | "Your account has been suspended. Contact support." |

**Document Upload:**

- Required document types:
  - Driver's License (front + back)
  - National ID / Passport
  - Vehicle Registration
  - Insurance Certificate
- Each slot: Camera capture or gallery upload
- **Endpoint:** `POST /api/v1/drivers/{id}/documents` (multipart form, one per document type)
- After all required documents are uploaded: `ApprovalStatus` automatically moves to `UnderReview`

**Admin Review (covered in §3.4.4):**

- Admin receives pending KYC count on dashboard
- Reviews each document individually
- Approve or Reject (with notes)
- When all documents approved → Admin can approve the driver

---

## 5. Admin Acting as Driver (Dual-Role)

### 5.1 UI Structure

When a user has both Admin and Driver roles:

- The admin interface includes an additional **"Driver Mode"** tab/section
- This section contains the full driver interface (online/offline toggle, trip execution, earnings)
- A clear visual indicator shows which mode is active
- A "Back to Admin" action returns to the admin interface
- Implemented dashboard shell behavior: dual-role users now open the Admin dashboard by default and can toggle between Driver Mode and Admin Mode without logging out.
- Implemented Phase 6 root-mode coordination: the Flutter app now uses a shared root-mode service so admin UI actions can switch the shell into Driver Mode after successful self-assignment.

### 5.2 Self-Assignment Workflow

1. Admin views a `PendingDriver` trip in Trip Management
2. Taps "Assign to Me" (shortcut) or selects themselves from the driver list
3. Backend: `POST /trips/{id}/assign { driverId: admin's own driver ID }`
4. `DriverAssigned` event fires — the Admin's own Driver Mode immediately shows the new trip
5. Admin switches to Driver Mode
6. Executes the trip through the standard 4-step lifecycle (En Route → Arrived → Start → Complete)

**Implementation note:** The dashboard assignment sheet now detects the authenticated user's `driverId` or linked driver `userId`, surfaces a dedicated "Assign to me" action, sends the normal manual-dispatch request, and switches the root shell to Driver Mode on successful assignment.

### 5.3 Forced Password Reset Guard

The backend exposes `POST /api/v1/auth/force-reset-password` for authenticated users carrying the `requires_password_reset` claim. The dashboard Flutter app now registers a dedicated `ForcePasswordResetScreen`, stores reset state in the auth BLoC, exchanges the reset response for a fresh JWT, refreshes the current profile, and blocks all authenticated routes until `requiresPasswordReset` is cleared.

---

## 6. Real-Time Communication (SignalR)

### 6.1 Two SignalR Hubs

| Hub                     | URL              | Purpose                               | Frequency                      |
| ----------------------- | ---------------- | ------------------------------------- | ------------------------------ |
| **TripHub**             | `/hubs/trips`    | Trip lifecycle events (state changes) | Event-driven (on state change) |
| **LocationTrackingHub** | `/hubs/location` | Driver GPS coordinates                | Every ~10 seconds per driver   |

### 6.2 TripHub Events

| Event Name         | Payload                            | Who Receives                 | When                      |
| ------------------ | ---------------------------------- | ---------------------------- | ------------------------- |
| `TripRequested`    | TripId, VehicleTypeId, PassengerId | Drivers in VehicleType group | New trip created          |
| `DriverAssigned`   | TripId, PassengerId, DriverId      | Trip group members           | Driver assigned to trip   |
| `DriverEnRoute`    | TripId, PassengerId, DriverId      | Trip group members           | Driver heading to pickup  |
| `DriverArrived`    | TripId, PassengerId, DriverId      | Trip group members           | Driver at pickup location |
| `TripStarted`      | TripId, PassengerId                | Trip group members           | Ride started              |
| `TripCompleted`    | TripId, PassengerId                | Trip group members           | Ride completed            |
| `TripCancelled`    | TripId, PassengerId                | Trip group members           | Trip cancelled            |
| `PaymentConfirmed` | TripId, PassengerId                | Trip group members           | Stripe payment succeeded  |
| `PaymentFailed`    | TripId, PassengerId, Reason        | Trip group members           | Stripe payment failed     |
| `TripRefunded`     | TripId, PassengerId, Amount        | Trip group members           | Refund processed          |

### 6.3 LocationTrackingHub Events

| Event/Method            | Direction       | Payload            | Who                                 |
| ----------------------- | --------------- | ------------------ | ----------------------------------- |
| `UpdateLocation`        | Client → Server | lat, lng           | Driver sends their GPS              |
| `DriverLocationUpdated` | Server → Client | driverId, lat, lng | Admin + assigned Passenger receives |

### 6.4 SignalR Groups

| Group Name           | Members                         | Purpose                     |
| -------------------- | ------------------------------- | --------------------------- |
| `User_{userId}`      | Single user                     | Personal notifications      |
| `Trip_{tripId}`      | Passenger + Driver + Admin      | Updates for a specific trip |
| `VehicleType_{code}` | All online drivers of that type | Broadcast new trip requests |

### 6.5 Authentication

SignalR connections authenticate via JWT passed as `?access_token=` query parameter (WebSockets cannot send Authorization headers). The server already supports this.

---

## 7. Push Notifications (FCM)

### 7.1 Firebase Console Setup

1. Open the Firebase Console for the existing Fat7i Taxi project
2. **Add App** — create entries for the Dashboard app (Android + iOS) with the Dashboard package name
3. Download `google-services.json` (Android) and `GoogleService-Info.plist` (iOS)
4. For iOS: Upload the Apple Push Notification (APNs) authentication key
5. **Server key:** The Firebase Admin SDK is already initialized on the server (it shares the same Firebase project). No additional server-side key setup needed.

### 7.2 FCM Token Registration

- On login and on token refresh, every app (Customer, Driver, Admin) sends its device token to: `PUT /api/v1/users/me/fcm-token`
- The server stores it in `User.FcmToken`

### 7.3 When FCM Push Notifications Are Sent

| Server Event     | Recipient | Title               | Body                                                             |
| ---------------- | --------- | ------------------- | ---------------------------------------------------------------- |
| `DriverAssigned` | Passenger | "Driver Assigned"   | "Your {VehicleType} ride is heading your way."                   |
| `DriverAssigned` | Driver    | "New Trip Assigned" | "You've been assigned trip {ReferenceCode}. Navigate to pickup." |
| `DriverEnRoute`  | Passenger | "Driver En Route"   | "Your {VehicleType} ride is on the way!"                         |
| `DriverArrived`  | Passenger | "Driver Arrived"    | "Your ride is outside."                                         |
| `TripCompleted`  | Passenger | "Trip Complete"     | "Your trip is complete. Fare: {Amount} {Currency}."              |
| `TripCancelled`  | Passenger | "Trip Cancelled"    | "Your trip has been cancelled."                                  |
| `TripCancelled`  | Driver    | "Trip Cancelled"    | "Trip {ReferenceCode} has been cancelled."                       |

> **Privacy Rule:** FCM payloads to Passengers MUST NOT contain driver name, phone, physical vehicle details, plate, or any personal info — only vehicle type and trip status.

### 7.4 Notification Handling

- **Foreground:** Show an in-app toast/banner (non-intrusive)
- **Background/Terminated:** System notification tray. Tapping the notification deep-links to the relevant screen (active trip for passengers, trip execution for drivers)

---

## 8. Customer Privacy Enforcement — CRITICAL

### 8.1 What the Customer NEVER Sees

- ❌ Driver's full name
- ❌ Driver's phone number
- ❌ Driver's profile photo
- ❌ Driver's rating or personal metrics
- ❌ Driver's personal address

### 8.2 What the Customer DOES See

- ✅ Vehicle Type name (e.g., "Standard", "XL Van")
- ✅ Trip status text ("Driver is on the way", "Driver has arrived")
- ✅ Driver's GPS position as a car icon on the map — no identity attached

### 8.3 Backend Enforcement

The backend must enforce this at the DTO level. Trip responses returned to Passenger-role users must contain a `PassengerTripDto` that excludes all driver personal fields. The customer app should never even receive driver identity data.

---

## 9. Complete API Endpoint Reference

### Existing Endpoints (Already Working)

| Method | Path                        | Auth     | Purpose                     |
| ------ | --------------------------- | -------- | --------------------------- |
| POST   | `/auth/login`               | Public   | Firebase phone auth login   |
| POST   | `/identity/tokens`          | Public   | Email/password login        |
| POST   | `/identity/tokens/refresh`  | Public   | Refresh JWT                 |
| POST   | `/identity/admins`          | Admin    | Register new admin          |
| GET    | `/app-config/client`        | Public   | Client config (Stripe)      |
| GET    | `/app-config/trip-discount` | Admin    | Get global discount         |
| PUT    | `/app-config/trip-discount` | Admin    | Update global discount      |
| GET    | `/drivers`                  | Admin    | List all drivers            |
| GET    | `/drivers/{id}`             | Admin    | Get driver detail           |
| POST   | `/drivers`                  | Admin    | Create driver               |
| PUT    | `/drivers/{id}`             | Admin    | Update driver               |
| DELETE | `/drivers/{id}`             | Admin    | Soft delete driver          |
| POST   | `/drivers/{id}/vehicle-type` | Admin    | Assign driver vehicle type  |
| GET    | `/vehicle-types`            | Public   | Vehicle catalog             |
| POST   | `/vehicle-types`            | Admin    | Create vehicle type         |
| PUT    | `/vehicle-types/{id}`       | Admin    | Update vehicle type pricing |
| DELETE | `/vehicle-types/{id}`       | Admin    | Remove vehicle type         |
| POST   | `/trips/quotes`             | Auth     | Get pricing quotes          |
| POST   | `/trips/request`            | Auth     | Request a trip              |
| POST   | `/trips/{id}/cancel`        | Auth     | Cancel a trip               |
| GET    | `/trips/me/active`          | Auth     | Get active trip             |
| GET    | `/trips/me/history`         | Auth     | Trip history                |
| GET    | `/trips/{id}`               | Auth     | Trip by ID                  |
| GET    | `/users/me`                 | Auth     | Current user profile        |
| POST   | `/users/me`                 | Auth     | Update profile              |
| POST   | `/maps/directions`          | Auth     | Get directions              |
| POST   | `/maps/search`              | Auth     | Search places               |
| POST   | `/maps/reverse-geocodings`  | Auth     | Reverse geocode             |
| POST   | `/webhooks/stripe`          | Public\* | Stripe webhooks             |

### New Endpoints (Must Be Built)

| Method | Path                                     | Auth         | Purpose                         |
| ------ | ---------------------------------------- | ------------ | ------------------------------- |
| POST   | `/auth/force-reset-password`             | Auth         | Force password change           |
| PUT    | `/users/me/fcm-token`                    | Auth         | Register FCM token              |
| GET    | `/users`                                 | Admin        | List all users                  |
| POST   | `/trips/{id}/en-route`                   | Driver       | Mark driver en route            |
| POST   | `/trips/{id}/arrive`                     | Driver       | Mark driver arrived             |
| POST   | `/trips/{id}/start`                      | Driver       | Start trip                      |
| POST   | `/trips/{id}/complete`                   | Driver       | Complete trip                   |
| POST   | `/trips/{id}/assign`                     | Admin        | Manual dispatch                 |
| GET    | `/trips`                                 | Admin        | All trips (paginated, filtered) |
| GET    | `/trips/{id}/details`                    | Admin        | Full trip details               |
| POST   | `/drivers/me/status`                     | Driver       | Set online/offline              |
| POST   | `/drivers/me/location`                   | Driver       | Update GPS location             |
| GET    | `/drivers/me/earnings`                   | Driver       | Earnings summary                |
| POST   | `/drivers/{id}/approve`                  | Admin        | Approve driver KYC              |
| POST   | `/drivers/{id}/suspend`                  | Admin        | Suspend driver                  |
| GET    | `/drivers/{id}/documents`                | Admin        | List KYC documents              |
| POST   | `/drivers/{id}/documents`                | Admin/Driver | Upload KYC document             |
| PUT    | `/drivers/{id}/documents/{docId}/review` | Admin        | Approve/reject document         |
| GET    | `/drivers/status`                        | Admin        | All drivers with live status    |
| GET    | `/audit-logs`                            | Admin        | View audit trail                |

**Total: 35 existing + 20 new = 55 endpoints**

---

## 10. Trip State Machine — Complete Lifecycle

```
   Passenger requests trip
          │
          ▼
   ┌──────────────┐
   │ AwaitingPayment│
   └──────┬───────┘
          │ Stripe payment succeeds (webhook)
          ▼
   ┌──────────────┐
   │ PendingDriver │ ◄── (or Scheduled, if scheduledAtUtc is set)
   └──────┬───────┘
          │ Admin manually assigns driver
          ▼
   ┌──────────────┐
   │ DriverAssigned │
   └──────┬───────┘
          │ Driver marks "en route"
          ▼
   ┌──────────────┐
   │ DriverEnRoute │  ◄── NEW
   └──────┬───────┘
          │ Driver marks "arrived"
          ▼
   ┌──────────────┐
   │ DriverArrived │  ◄── NEW
   └──────┬───────┘
          │ Driver starts trip
          ▼
   ┌──────────────┐
   │  InProgress   │
   └──────┬───────┘
          │ Driver completes trip
          ▼
   ┌──────────────┐
   │  Completed    │
   └──────────────┘

   ──── Side Branches ────

   Any pre-InProgress state ──► Cancelled (passenger/admin cancels)
   Cancelled/Completed    ──► Refunded  (Stripe refund processed)
   AwaitingPayment        ──► PaymentFailed (Stripe card rejected)
```

---

## 11. Security Checklist

- [ ] All Admin endpoints guarded by `Authorize(Roles = "Admin")`
- [ ] All Driver trip-action endpoints validate the caller IS the assigned driver
- [ ] Customer trip DTOs NEVER contain driver identity fields
- [ ] FCM payloads to passengers NEVER contain driver personal info
- [ ] JWT uses HMAC-SHA256 with server-side secret
- [ ] Refresh tokens are cryptographically random (32 bytes)
- [ ] Stripe webhook validates cryptographic signature
- [ ] Rate limiting: 100 requests/min per IP (already active)
- [ ] `RequiresPasswordReset` flag blocks all routes except force-reset
- [ ] KYC document file URLs are not publicly accessible
- [ ] Audit log captures all Admin mutations atomically
- [ ] Driver cannot go Online unless `ApprovalStatus == Approved` AND vehicle type assigned
- [ ] Global query filter ensures soft-deleted entities are never returned
