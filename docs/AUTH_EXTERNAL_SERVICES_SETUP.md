# Auth External Services — Setup & Operations Guide

This guide covers everything you must configure **manually** to run the new
backend-owned authentication after the code is deployed. Firebase phone OTP has
been replaced by a backend-controlled OTP system:

- **CM.com** — SMS delivery (phone OTP only).
- **Titan.email** — SMTP delivery (email OTP, verification/welcome mails).
- **Firebase** — Google Sign-In only (backend verifies the Google ID token via the
  already-integrated Firebase Admin SDK).

The backend owns **all** OTP logic (generation, hashing, expiry, resend cooldown,
attempt limits, verification, account creation/linking). CM.com and Titan are dumb
delivery pipes; no OTP-as-a-service is used. **No provider secret is ever shipped to
the mobile apps.**

> See also: [`AUTH_FLOW.md`](./AUTH_FLOW.md) for the exact login/sign-up behavior
> and the verified-identity uniqueness rules.

---

## 0. Configuration model

All settings are bound with the ASP.NET Core Options pattern. Provide real values via
**user-secrets** (local dev) or **environment variables** (production). Never commit
real secrets — `appsettings.json` ships with empty placeholders.

Environment-variable nesting uses a **double underscore** (`__`). Examples:

| Setting (appsettings path) | Environment variable |
| --- | --- |
| `Otp:HashSecret` | `Otp__HashSecret` |
| `Sms:CmCom:ProductToken` | `Sms__CmCom__ProductToken` |
| `Sms:CmCom:Sender` | `Sms__CmCom__Sender` |
| `Email:Titan:Password` | `Email__Titan__Password` |
| `AppSettings:FirebaseCredentials` | `AppSettings__FirebaseCredentials` |

### Full environment-variable reference

```
# --- OTP policy (backend-owned) ---
Otp__CodeLength=6
Otp__ExpiryMinutes=5
Otp__MaxAttempts=5
Otp__ResendCooldownSeconds=60
Otp__OtpRetentionDays=30
Otp__HashSecret=<long-random-secret-32+chars>     # REQUIRED — HMAC key for OTP hashes

# --- CM.com SMS ---
Sms__CmCom__BaseUrl=https://gw.messaging.cm.com/
Sms__CmCom__ProductToken=<cm-product-token>        # REQUIRED
Sms__CmCom__Sender=Fat7i                       # sender name / originator

# --- Titan.email SMTP ---
Email__Titan__Host=smtp.titan.email
Email__Titan__Port=465                               # 465 = SSL, 587 = STARTTLS
Email__Titan__UseSsl=true                            # true for 465, false for 587
Email__Titan__Username=<full-from-email>             # REQUIRED (usually = FromEmail)
Email__Titan__Password=<mailbox-or-app-password>     # REQUIRED
Email__Titan__FromEmail=no-reply@yourdomain.com      # REQUIRED
Email__Titan__FromName=Fat7i
Email__Titan__ReplyToEmail=support@yourdomain.com    # optional

# --- Firebase (Google Sign-In verification) — already used for FCM ---
AppSettings__FirebaseCredentials=<service-account-json-or-file-path>
```

### Local dev (`dotnet run`) vs Docker (`.env`)

There are two config mechanisms, for two different runtimes — **use both**; neither replaces
the other:

- **`dotnet user-secrets`** — local development only. Loaded solely when you run the API
  directly (`dotnet run` / IDE) in the **Development** environment. It does **not** work in
  containers. Use it for §4 below.
- **`.env` + Docker Compose** — the runtime/production mechanism. Compose auto-loads `.env`,
  injects the values as container env vars, and ASP.NET's environment-variable provider reads
  them (the `__` nesting). Use it for Docker (dev **and** prod). This is how JWT, Stripe,
  Firebase, etc. are already configured.

`.env` is **gitignored** and must be created from `.env.example`. On a server, restrict it
with `chmod 600 .env`. For stronger isolation you can later switch to Docker secrets or a
secrets manager (Vault / AWS/GCP Secrets Manager), but `.env` is the standard, pragmatic
choice for this stack.

### `.env` (Docker) variable-name mapping

The Docker compose files map UPPER_SNAKE `.env` names → the `Section__Key` env vars the app
reads (see `docker-compose.override.yml` for dev and `docker-compose.prod.yml` for prod).
Add these to your `.env` (documented in `.env.example`):

| `.env` variable | App config key | Required |
| --- | --- | --- |
| `OTP_HASH_SECRET` | `Otp__HashSecret` | **yes** |
| `OTP_CODE_LENGTH` / `OTP_EXPIRY_MINUTES` / `OTP_MAX_ATTEMPTS` / `OTP_RESEND_COOLDOWN_SECONDS` / `OTP_RETENTION_DAYS` | `Otp__*` | no (defaults) |
| `CM_PRODUCT_TOKEN` | `Sms__CmCom__ProductToken` | **yes** |
| `CM_SENDER` | `Sms__CmCom__Sender` | no (default `Fat7i`) |
| `CM_BASE_URL` | `Sms__CmCom__BaseUrl` | no (default gateway) |
| `TITAN_USERNAME` | `Email__Titan__Username` | **yes** |
| `TITAN_PASSWORD` | `Email__Titan__Password` | **yes** |
| `TITAN_FROM_EMAIL` | `Email__Titan__FromEmail` | **yes** |
| `TITAN_FROM_NAME` | `Email__Titan__FromName` | no |
| `TITAN_HOST` / `TITAN_PORT` / `TITAN_USE_SSL` | `Email__Titan__Host`/`Port`/`UseSsl` | no (defaults) |
| `TITAN_REPLY_TO_EMAIL` | `Email__Titan__ReplyToEmail` | no |

Docker run commands:
```bash
# Dev  (auto-merges docker-compose.override.yml)
docker compose up -d
# Prod
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```
Once the variables are present in `.env`, the auth settings are injected into the container
automatically — no code or appsettings changes needed.

---

## 1. CM.com (SMS)

CM.com sends the phone OTP SMS via the **Business Messaging API**.

### 1.1 Get the product token
1. Sign in to the CM.com platform: <https://www.cm.com/app/>.
2. Open **Channels → Gateway (SMS)** (a.k.a. the Business Messaging / Gateway API page).
3. Copy the **Product Token** (a GUID-like value). This authenticates every request.
   → set `Sms__CmCom__ProductToken`.

### 1.2 Sender name (originator)
- The `Sender` is what recipients see as the "from". It can be an **alphanumeric sender
  ID** (e.g. `Fat7i`, max 11 chars, no spaces) or a purchased number.
- Alphanumeric sender IDs are **not supported in every country** (notably not for
  2-way in some regions) and may require pre-registration with CM.com. For the
  Netherlands, an alphanumeric sender ID generally works for one-way OTP SMS.
- Configure it via `Sms__CmCom__Sender`. Confirm allowed sender IDs with your CM.com
  account manager for each destination country.

### 1.3 API used
- Endpoint: `POST https://gw.messaging.cm.com/v1.0/message` (global Cloudflare gateway;
  configurable via `Sms__CmCom__BaseUrl`).
- The backend forces `allowedChannels: ["SMS"]` so messages are **SMS only** (never
  WhatsApp/OTT). This is **not** CM.com OTP-as-a-Service — we only send plain text.
- Implemented with `IHttpClientFactory` (`CmComSmsSender`), behind the `ISmsSender`
  abstraction, so the provider can be swapped without touching auth logic.

### 1.4 Test SMS flow
1. Set the env vars above and start the API.
2. Trigger a phone sign-up OTP:
   ```bash
   curl -X POST https://<your-api>/api/v1/auth/phone/signup/otp \
     -H "Content-Type: application/json" \
     -d '{"phone":"+31612345678"}'
   ```
   Expected: `200` with `{ "otpRequestId": "...", "expiresInSeconds": 300, "resendAvailableInSeconds": 60 }`
   and an SMS arriving at the phone.
3. Verify:
   ```bash
   curl -X POST https://<your-api>/api/v1/auth/phone/signup/otp/verify \
     -H "Content-Type: application/json" \
     -d '{"otpRequestId":"<from-step-2>","code":"<code-from-sms>"}'
   ```

### 1.5 Delivery status callbacks (optional)
Not implemented in this iteration. The provider message reference is stored on the OTP
row (`ProviderMessageId`) for debugging. If you later need delivery receipts, add a
status-callback endpoint and configure the callback URL in CM.com — the SMS abstraction
is designed to accommodate this without changing auth logic.

---

## 2. Titan.email (SMTP)

Titan sends email OTP and simple transactional mails via SMTP (MailKit).

### 2.1 SMTP settings
| Setting | Value |
| --- | --- |
| Host | `smtp.titan.email` |
| Port | `465` (implicit SSL) **or** `587` (STARTTLS) |
| Security | SSL for 465, STARTTLS for 587 |
| Username | your full mailbox address (e.g. `no-reply@yourdomain.com`) |
| Password | the mailbox password (or app password if you enable one) |

Set `Email__Titan__Port` and `Email__Titan__UseSsl` together: `465`+`true`, or `587`+`false`.

### 2.2 Enable sending
- Ensure the mailbox exists in your Titan control panel (via your host, e.g. Hostinger /
  your domain registrar) and that SMTP sending is permitted for it.
- Some hosts require enabling "third-party app / SMTP access" for the mailbox — check
  the Titan mailbox settings if authentication fails.

### 2.3 Recommended DNS records (deliverability)
Set these on the sending domain so OTP mails don't land in spam:
- **SPF**: add Titan to your `TXT` SPF record, e.g. `v=spf1 include:spf.titan.email ~all`.
- **DKIM**: enable DKIM in the Titan panel and publish the provided `CNAME`/`TXT` records.
- **DMARC**: publish a `TXT` record at `_dmarc.yourdomain.com`, e.g.
  `v=DMARC1; p=quarantine; rua=mailto:dmarc@yourdomain.com`.

(Exact SPF include / DKIM selectors are shown in your Titan/host control panel — use those.)

### 2.4 Test email flow
```bash
# Request an email sign-up OTP (sends an email via Titan)
curl -X POST https://<your-api>/api/v1/auth/email/signup/otp \
  -H "Content-Type: application/json" \
  -d '{"email":"you@example.com"}'
```
Expected: `200` with an `otpRequestId` and an email arriving in the inbox.

---

## 3. Firebase — Google Sign-In

Firebase is used **only** for Google Sign-In. The backend verifies the Google ID token
using the Firebase Admin SDK (already integrated for FCM), so **no new backend Firebase
setup is required** beyond the existing `AppSettings:FirebaseCredentials` service account.

### 3.1 Enable Google as a sign-in provider
1. Firebase Console → **Authentication → Sign-in method → Google → Enable**.
2. Set the project support email.

### 3.2 Mobile app configuration (customer app `customertaxi`)
- **Android**: add the app's **SHA-1 and SHA-256** fingerprints to the Firebase Android
  app (Project settings → Your apps → Android → *Add fingerprint*). Re-download
  `google-services.json` after adding them. Google Sign-In fails on Android without the
  correct SHA fingerprints.
  ```bash
  # debug fingerprint
  keytool -list -v -alias androiddebugkey -keystore ~/.android/debug.keystore -storepass android -keypass android
  # release fingerprint comes from your upload/release keystore (and Play App Signing)
  ```
- **iOS**: add the reversed client ID URL scheme to the iOS app and re-download
  `GoogleService-Info.plist`. Ensure the `REVERSED_CLIENT_ID` is in the URL Types.
- The customer app uses the `google_sign_in` package; the OAuth client IDs are managed by
  the Firebase/Google config files (`google-services.json` / `GoogleService-Info.plist`).

### 3.3 Backend verification
- No new secret needed — the existing `AppSettings:FirebaseCredentials` (service account
  JSON or file path) is reused. `IFirebaseAuthService.VerifyIdTokenAndGetIdentityAsync`
  verifies the token and extracts the Google email/uid.

---

## 4. Local development

> This section is for running the API **directly** (`dotnet run` / IDE). If you run the dev
> stack in **Docker** (`docker compose up -d`), configure `.env` instead (see §0) — user-secrets
> are not visible inside containers.

1. Set secrets with **dotnet user-secrets** (from `src/Taxi.Api`):
   ```bash
   dotnet user-secrets set "Otp:HashSecret" "dev-only-long-random-secret"
   dotnet user-secrets set "Sms:CmCom:ProductToken" "<cm-token>"
   dotnet user-secrets set "Email:Titan:Username" "no-reply@yourdomain.com"
   dotnet user-secrets set "Email:Titan:Password" "<password>"
   dotnet user-secrets set "Email:Titan:FromEmail" "no-reply@yourdomain.com"
   ```
2. **Test SMS without spamming real users:** during development, use your own phone
   number, or ask CM.com for test credentials. Rate limiting (5 OTP requests/min/IP) and
   the 60s per-recipient resend cooldown protect against accidental spam.
3. **Test email:** use a real inbox you control (or a catch-all like Mailtrap by pointing
   `Email__Titan__Host/Port` at the Mailtrap SMTP sandbox for dev).
4. **Test Google login:** run the customer app on a device/emulator with the Firebase
   config files in place and the SHA fingerprint registered.

> OTP codes are **never** logged in plain text and are **never** returned in API
> responses. There is no "return code in response" dev shortcut in this implementation.

---

## 5. Production deployment

1. Provide all env vars from §0 on the server/VPS. With this stack that means populating
   **`.env`** from `.env.example` (see the mapping table in §0), then
   `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d` — Compose injects
   them into the `taxi.api` container. (`chmod 600 .env`.)
2. `Otp__HashSecret` must be a strong, stable secret. **Changing it invalidates all
   in-flight OTPs** (existing codes can no longer be verified) — rotate only during a
   quiet window.
3. Ensure outbound network access from the API host to `gw.messaging.cm.com:443` and
   `smtp.titan.email:465` (or `:587`).
4. Confirm SPF/DKIM/DMARC are live on the sending domain before launch.
5. Webhook/callback URLs: none required (SMS delivery callbacks not implemented).

### Security checklist before release
- [ ] `Otp__HashSecret` set to a strong random value (32+ chars) and stored as a secret.
- [ ] CM.com product token & Titan password set as secrets (not in `appsettings.json`).
- [ ] No provider secrets present in either Flutter app bundle.
- [ ] SPF / DKIM / DMARC configured for the sending domain.
- [ ] Firebase Google provider enabled; SHA fingerprints registered for Android release.
- [ ] Rate limiting active (`OtpRequest` policy) and reachable behind the proxy
      (`ForwardedHeaders` configured so per-IP limiting sees the real client IP).
- [ ] Logs reviewed: no OTP codes or secrets present.
- [ ] EF migration `AddOtpAndVerifiedIdentity` applied; duplicate-email report reviewed
      (see §6.3 and `AUTH_FLOW.md`).

---

## 6. Troubleshooting

### SMS not received
- Verify `Sms__CmCom__ProductToken` is correct (a wrong token → provider rejects; the
  API returns a safe "could not send code" error and logs the CM.com status/body).
- Check the destination country allows your alphanumeric `Sender` (some require a
  registered sender or a number). Try a numeric sender if alphanumeric is blocked.
- Confirm the phone is in international format; the backend normalizes `0…` (NL) to
  `+31…`, but non-NL national numbers should be entered with a `+` country code.
- Check the API logs for `CM.com SMS send failed … Status=…` (recipient is masked).

### Email not received
- Check spam. Fix SPF/DKIM/DMARC (§2.3) if mails are filtered.
- Verify `Host`/`Port`/`UseSsl` pairing (465+SSL or 587+STARTTLS).

### "SMTP authentication failed"
- Username must be the **full** email address. Re-check the password / enable app or
  third-party SMTP access on the Titan mailbox. Try the alternate port/security pairing.

### Google login failed
- Android: missing/incorrect **SHA fingerprint**, or stale `google-services.json`.
- Backend "The Google sign-in could not be verified": the ID token is invalid/expired, or
  the Firebase service account (`AppSettings:FirebaseCredentials`) is wrong for the
  project that issued the token.
- "The Google account did not provide an email address": the Google account has no email
  scope/email — cannot be used to create an account.

### OTP expired
- Codes live `Otp__ExpiryMinutes` (default 5). Request a new one; do not reuse old codes.

### Too many attempts / cooldown
- After `Otp__MaxAttempts` (default 5) wrong tries, the code locks — request a new one.
- Resend is blocked for `Otp__ResendCooldownSeconds` (default 60) per recipient; the
  per-IP `OtpRequest` limiter also caps requests (5/min). Wait and retry.

### CM.com product token rejected
- Double-check the token from the Gateway API page; ensure no trailing whitespace and
  that it belongs to the correct CM.com account/environment.

### Titan SMTP authentication failed
- See "SMTP authentication failed" above.

---

## 7. What the code added (reference)

- **Endpoints** (all under `/api/v1/auth`, see `AuthController`):
  `phone/login/otp` (+`/verify`), `phone/signup/otp` (+`/verify`),
  `email/login/otp` (+`/verify`), `email/signup/otp` (+`/verify`),
  `google`, `register/complete`, `phone/verify/otp` (+`/verify`, authorized),
  `account/fresh-start` (authorized). Legacy `sessions`/`login` kept (deprecated).
- **NuGet added:** `MailKit` (Titan SMTP). `FirebaseAdmin` was already present.
- **DB migration:** `AddOtpAndVerifiedIdentity` — adds the `OtpCodes` table and
  `User.GoogleId`, `User.IsPhoneVerified`, `User.IsEmailVerified`, `User.ProfileResetAtUtc`;
  swaps the phone unique index for a partial one, adds partial unique indexes for verified
  email and GoogleId; backfills existing users to `IsPhoneVerified = true`; emits a
  duplicate-email `RAISE NOTICE` report to the DB server log.
