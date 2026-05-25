# 04. API & Endpoints Layer (`Taxi.Api`) Exhaustive Analysis

The API layer is the public boundary of the backend. It uses ASP.NET Core 8 Web API, relies strictly on RESTful principles, and acts entirely as a thin wrapper around the MediatR pipeline. 

There is **zero business logic** in the controllers.

---

## 1. Request Pipeline & Middleware (`DependencyInjection.cs`)

The API is heavily fortified with production-grade middleware:

### Rate Limiting (`AddAppRateLimiting`)
- Implements a **Sliding Window Limiter**.
- Rules: Maximum of 100 requests per minute per IP address, with a small queue of 10.
- Returns `429 Too Many Requests` when triggered, protecting against DDoS or brute-force attacks.

### Forwarded Headers (`AddAppForwardedHeaders`)
- Configured to trust reverse proxies (like Nginx, Cloudflare, or AWS ALB).
- Extracts the real client IP from `X-Forwarded-For` so Rate Limiting and Audit Logging work correctly.

### Output Caching (`AddAppOutputCaching`)
- Built-in ASP.NET Core 8 Output Caching is used for high-read, low-write data (like the Vehicle Catalog).
- **VaryByHeader**: It specifically caches responses segmented by the `Accept-Language` header. (An English user gets the English cache, an Arabic user gets the Arabic cache).

### Validation Translation (`AddValidation`)
- Intercepts `ModelState` errors (thrown by FluentValidation in the Application layer).
- Dynamically translates the error keys into the user's preferred language using `IStringLocalizer<SharedResource>`.
- Formats the response precisely to RFC 7807 standard `ValidationProblemDetails` (HTTP 400).

### Exception Handling & Logging
- **`GlobalExceptionHandler`**: Catches unhandled exceptions, prevents server crashes, and returns a sanitized HTTP 500 `ProblemDetails` response.
- **`RequestLogContextMiddleware`**: Grabs the `TraceIdentifier` of the HTTP request and pushes it into Serilog's `LogContext` as `CorrelationId`. This ensures every database query, domain event, and API response belonging to the same request can be grouped in log aggregators (like Seq or Datadog).

---

## 2. API Versioning & OpenAPI (Swagger)

- **Versioning**: Uses `Asp.Versioning.Mvc`. Routes are strictly mapped using `api/v{version:apiVersion}/...` with a default of `1.0`.
- **OpenAPI Transformers**:
  - `AcceptLanguageOperationTransformer`: Custom code that adds an `Accept-Language` dropdown to the Swagger UI, allowing developers to test the 9 supported languages instantly.
  - `BearerSecuritySchemeTransformer`: Automatically attaches JWT lock icons to secured endpoints in Swagger.

---

## 3. Core Services

- **`CurrentUser`**: Implements `IUser`. Reaches into `IHttpContextAccessor` to extract the `ClaimTypes.NameIdentifier` (the User ID) from the JWT.
- **`LanguageContext`**: Implements `ILanguageContext`. Extracts the validated `TwoLetterISOLanguageName` set by the `RequestLocalizationMiddleware`. Used by the Application layer to execute localized database projections.

---

## 4. Problem Details Mapping (`ProblemExtensions.cs`)

This is the bridge between Domain `Result<T>` and HTTP.
When a Controller receives an `Error` from MediatR, it calls `errors.ToProblem(this)`.

| Domain Error Type | HTTP Status Code |
| :--- | :--- |
| `ErrorKind.NotFound` | 404 Not Found |
| `ErrorKind.Conflict` | 409 Conflict |
| `ErrorKind.Validation`| 400 Bad Request (ValidationProblemDetails) |
| `ErrorKind.Unauthorized`| 401 Unauthorized |
| `ErrorKind.Forbidden` | 403 Forbidden |

---

## 5. Controller Breakdown

### 1. `AuthController` & `IdentityController`
- `POST /auth/login`: Accepts `FirebaseIdToken`. Silent registration. Returns App JWT.
- `POST /identity/tokens/refresh`: Sliently rotates expiring Access Tokens using the long-lived Refresh Token.
- `POST /identity/admins`: Secured by `[Authorize(Roles = "Admin")]`. Registers new backend staff.

### 2. `TripsController` (The Engine)
- `POST /trips/quotes`: Public endpoint. Accepts `[Latitude, Longitude]` array. Returns live pricing matrix.
- `POST /trips/request`: Requires valid `QuoteId`. Generates `StripePaymentIntent` (Client Secret).
- `POST /trips/{id}/cancel`: Refunds/cancels active payments and updates Domain state.
- `GET /trips/me/active`: Polling endpoint (though SignalR is preferred) to get the user's current trip status.

### 3. `WebhooksController`
- `POST /webhooks/stripe`: 
  - Uses `[AllowAnonymous]` because Stripe doesn't have a JWT.
  - Reads the raw `Request.Body` string.
  - Extracts the `Stripe-Signature` header.
  - Returns 200 OK immediately for successfully processed webhooks so Stripe stops retrying. Returns 400 Bad Request if the cryptographic signature doesn't match the configured `WebhookSecret`.

### 4. `VehicleTypesController`
- `GET /vehicle-types`: Employs `[OutputCache(Duration = 60)]`. Serves the live catalog of vehicles (Standard, Premium) translated to the user's language.

### 5. `AppConfigController`
- `GET /app-config/client`: Extremely important bootstrap endpoint. Tells the mobile app if `StripeEnabled` is true, and passes down the `StripePublishableKey` so the app can initialize the Stripe SDK.
