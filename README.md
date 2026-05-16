# Taxi.Server

.NET 10 Clean-Architecture taxi backend: CQRS, JWT auth, Stripe payments, real-time SignalR notifications, and a Blazor WebAssembly admin client.

---

## Quick start

**Prerequisites**: .NET 10 SDK, Docker Desktop

```bash
# 1. Provision Postgres + Seq
docker-compose up -d

# 2. Run the API (migrations + Admin seeding run automatically on startup)
dotnet run --project src/Taxi.Api
```

Default admin credentials: `admin@taxi.com` / `Admin123!`

---

## Tech stack

|               |                                                                                        |
| ------------- | -------------------------------------------------------------------------------------- |
| Runtime       | .NET 10                                                                                |
| ORM           | EF Core 9 (Npgsql / PostgreSQL) — Code-First, UTC strategy, JSONB for bilingual fields |
| CQRS          | MediatR + FluentValidation pipeline behaviors                                          |
| Auth          | ASP.NET Identity + JWT + refresh tokens                                                |
| Payments      | Stripe.net — PaymentIntent + webhook handler                                           |
| Real-time     | SignalR (driver assignment, trip status push)                                          |
| Observability | Serilog → Seq, OpenTelemetry (traces + metrics)                                        |
| Caching       | Hybrid Cache (multi-tier, culture-partitioned)                                         |
| Admin client  | Blazor WebAssembly                                                                     |
| API docs      | Scalar + Swagger (OpenAPI)                                                             |

---

## Solution layout

```
src/
├── Taxi.Domain          # Aggregates, Value Objects, Domain Events, Result<T>
├── Taxi.Contracts       # DTOs, Requests, Responses, LocalizationKeys
├── Taxi.Application     # CQRS handlers (Vertical Slices), validators, mappers
├── Taxi.Infrastructure  # EF Core, Identity, Stripe, SignalR, Background Jobs
├── Taxi.Api             # REST Controllers, middleware, OpenAPI, host
└── Taxi.Client          # Blazor WebAssembly admin frontend
tests/
├── Taxi.Domain.Tests
├── Taxi.Application.Tests
├── Taxi.Api.IntegrationTests
├── Taxi.Api.EndToEndTests
└── Taxi.Testing         # Shared fixtures, builders, DbSet mocks
```

Dependencies flow strictly inward: `Api → Application → Domain ← Contracts ← Infrastructure`.

---

## Domain at a glance

**Aggregates**: User, Driver, Trip, Vehicle, VehicleType, Payment, PricingQuote, PromoCode, PassengerPaymentMethod, AppConfig, AuditLog, Notification, RefreshToken.

**Trip state machine**

```
AwaitingPayment → PendingDriver (or Scheduled)
PendingDriver   → DriverAssigned → Started → Completed
                                             → Cancelled
                → PaymentFailed
                → Cancelled
Completed       → Refunded
```

**Payment state machine**

```
Pending → Completed
        → Failed
        → Refunded
```

Bilingual fields (name, description, etc.) use the `LocalizedText` value object stored as JSONB. Raw `string` properties for translatable text are forbidden.

---

## API endpoints

| Group         | Routes                                                                                                         |
| ------------- | -------------------------------------------------------------------------------------------------------------- |
| Auth          | `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/revoke`                                      |
| Identity      | `GET/PUT /api/identity/me`, `POST /api/identity/change-password`                                               |
| Trips         | `POST /api/trips`, `GET /api/trips`, `GET /api/trips/{id}`, `POST /api/trips/{id}/cancel`                      |
| Drivers       | `GET /api/drivers`, `POST /api/drivers`, `PUT /api/drivers/{id}`, `PATCH /api/drivers/{id}/status`             |
| Vehicles      | CRUD under `/api/vehicles`                                                                                     |
| Vehicle types | CRUD under `/api/vehicle-types`                                                                                |
| Users         | `GET /api/users`, `GET /api/users/{id}`, `DELETE /api/users/{id}`                                              |
| Maps          | `GET /api/maps/search`, `GET /api/maps/reverse-geocode`, `GET /api/maps/route`, `GET /api/maps/pricing-quotes` |
| Config        | `GET /api/config` — returns `ClientConfigResponse` (includes `stripeEnabled` flag)                             |
| Webhooks      | `POST /api/webhooks/stripe` — Stripe signature-validated event handler                                         |

---

## Stripe integration

Fare charging happens at booking confirmation, not at trip completion.

1. **RequestTripCommandHandler** reads `ClientConfig.StripeEnabled`. If enabled, it creates a `Payment` aggregate and calls `IStripePaymentService.CreatePaymentIntentAsync`. The response includes `clientSecret` + `publishableKey` — returned to the Flutter app, which presents the Payment Sheet.

2. **Webhook handler** (`POST /api/webhooks/stripe`) is `[AllowAnonymous]`; authentication is the `Stripe-Signature` header validated by `IStripeWebhookValidator`. Four event kinds are handled:

   | Event                           | Effect                                                        |
   | ------------------------------- | ------------------------------------------------------------- |
   | `payment_intent.succeeded`      | Payment → Completed; Trip → confirms payment + assigns driver |
   | `payment_intent.payment_failed` | Payment → Failed; Trip → PaymentFailed                        |
   | `payment_intent.canceled`       | Payment → Failed; Trip → PaymentFailed                        |
   | `charge.refunded`               | Payment → Refunded; Trip → Refunded                           |

   All handlers are idempotent — re-delivered webhooks short-circuit when state already matches. Unknown event types return HTTP 200 so Stripe does not retry.

For the full Stripe architecture see [ARCHITECTURE.md — Stripe Payment Architecture](ARCHITECTURE.md#stripe-payment-architecture).

---

## Configuration

Key `appsettings.json` / User Secrets entries:

```json
{
  "ConnectionStrings": { "Default": "..." },
  "Jwt": { "Secret": "...", "Issuer": "...", "Audience": "..." },
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_...",
    "TestMode": true
  },
  "FeatureFlags": { "StripeEnabled": true }
}
```

Local dev: `dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."` from `src/Taxi.Api`.

---

## Testing

```bash
dotnet test
```

Five test projects: Domain unit tests, Application unit tests (includes Stripe path: webhook handler + request-trip handler), API integration tests, API end-to-end tests, shared Testing helpers.

---

## Further reading

| Document                                                                                       | What it covers                                                                    |
| ---------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| [ARCHITECTURE.md](ARCHITECTURE.md)                                                             | Layer constitution, CQRS philosophy, Golden Workflow, Stripe payment architecture |
| [Domain_Layer_Blueprint.md](src/Taxi.Domain/Domain_Layer_Blueprint.md)                         | Aggregate rules, Value Objects, Domain Events                                     |
| [Application_Layer_Blueprint.md](src/Taxi.Application/Application_Layer_Blueprint.md)          | CQRS handler patterns, pipeline behaviors                                         |
| [Infrastructure_Layer_Blueprint.md](src/Taxi.Infrastructure/Infrastructure_Layer_Blueprint.md) | EF Core, Identity, JSONB, background jobs                                         |
| [Api_Layer_Blueprint.md](src/Taxi.Api/Api_Layer_Blueprint.md)                                  | Controller patterns, Result → ProblemDetails mapping                              |
| [Contracts_Layer_Blueprint.md](src/Taxi.Contracts/Contracts_Layer_Blueprint.md)                | DTO rules, LocalizationKeys registry                                              |
| [Client_Layer_Blueprint.md](src/Taxi.Client/Client_Layer_Blueprint.md)                         | Blazor auth state, hub client, token refresh                                      |
