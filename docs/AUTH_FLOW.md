# Auth Flow & Identity Rules

Behavioral reference for the backend-owned authentication. For provider setup and
environment variables see [`AUTH_EXTERNAL_SERVICES_SETUP.md`](./AUTH_EXTERNAL_SERVICES_SETUP.md).

## Sign-up / login methods (customer app)

Three methods, each in login and sign-up mode:

| Method | Sign-up | Login |
| --- | --- | --- |
| **Phone** | SMS OTP **required** (verified phone = the account identity) | SMS OTP; requires an existing **verified-phone** account |
| **Email** | Email OTP (passwordless) proves the email; then name + phone collected | Email OTP; requires an existing **verified-email** account |
| **Google** | Firebase Google token verified by backend; then name + phone collected | Firebase Google token; requires an existing account |

The **dashboard driver app** uses **phone login only** (phone-login endpoints). Admin
username/password login is unchanged.

## Phone verification rules

- **Phone-first sign-up requires SMS verification** — the verified phone *is* the identity.
- **Phone login requires a verified phone.** A phone that was entered during Google/email
  sign-up but never verified **cannot** be used to log in until it is verified.
- **Google/email sign-up requires the user to enter a phone, but verification can be
  skipped.** After `register/complete`, the app offers **Verify now** / **Skip for now**:
  - *Verify now* → authorized SMS OTP flow → `IsPhoneVerified = true`.
  - *Skip for now* → `IsPhoneVerified = false`; the app shows a persistent warning
    (right after entering the app and in the Profile tab) with a **Verify now** action.
- **Unverified phone numbers are contact/profile data, not login identities.**

## Verified-identity uniqueness (critical)

Enforced by **partial unique indexes** (PostgreSQL) so unverified copies never block the
real owner:

- **Verified phones are unique**: `UNIQUE(Phone) WHERE IsPhoneVerified = true AND DeletedAtUtc IS NULL`.
  Duplicate *unverified* phones are allowed and are never usable for login.
- **Verified emails are unique**: `UNIQUE(Email) WHERE IsEmailVerified = true AND Email IS NOT NULL AND DeletedAtUtc IS NULL`.
  Unverified emails are contact data only.
- **Google identity is unique**: `UNIQUE(GoogleId) WHERE GoogleId IS NOT NULL AND DeletedAtUtc IS NULL`.

### Conflict behavior
- Verifying a phone already **verified** on another active account → `409 Conflict`
  ("This phone number is already verified on another account"), no OTP sent. The **first**
  account to verify a number becomes its owner; others keep it as unverified contact data.
- `register/complete` with an email already **verified** elsewhere → `409 Conflict`
  ("You already have an account").
- Google sign-in matches a returning user by `GoogleId`, else by an existing **verified
  email** (Google emails are provider-verified, so linking is safe).
- Phone/email **sign-up** that hits an existing account returns the session flagged
  `accountAlreadyExists = true` so the app can say "You already have an account".

## "Start fresh" (soft reset)

`POST /api/v1/auth/account/fresh-start` (authorized) wipes the user's profile
(name → placeholder, clears email/photo/home) and stamps `ProfileResetAtUtc`. **No hard
delete** — the account id, phone verification, and all historical rows (trips, invoices)
are retained.

- The `ProfileResetAtUtc` epoch hides pre-reset trips **only** in the customer-facing
  "my trips/history" list.
- **Admin, accounting, payment, invoice, refund and audit queries ignore this epoch** and
  continue to see every row. (Implemented only in `GetPassengerTripsQueryHandler`.)

## Pre-existing duplicate-email remediation

Before this change, `User.Email` was optional and non-unique, so pre-existing rows may
share an email. The migration:
- Backfills **all** existing users to `IsEmailVerified = false` (email login did not exist
  before), so the partial email unique index never conflicts with legacy data.
- Emits a `RAISE NOTICE` report of duplicate emails to the **DB server log** during the
  migration (it does **not** pick a winner).

To remediate manually, list duplicates:
```sql
SELECT lower("Email") AS email, COUNT(*) AS accounts,
       array_agg("Id") AS user_ids
FROM "DomainUsers"
WHERE "Email" IS NOT NULL AND "DeletedAtUtc" IS NULL
GROUP BY lower("Email")
HAVING COUNT(*) > 1
ORDER BY accounts DESC;
```
Decide which account should own each email, then let that user prove ownership via the
email OTP flow (which sets `IsEmailVerified = true`). Only one account per email can ever
become verified.

## Security properties

- OTP: secure random 6-digit codes, **HMAC-SHA256 hashed at rest** (never stored/logged in
  plain), 5-minute expiry, max 5 attempts, single-use, 60s resend cooldown, per-IP request
  rate limiting, UTC timestamps, metadata (channel, purpose, recipient, ip, device,
  provider message id) for debugging. Expired/consumed rows are purged after
  `Otp:OtpRetentionDays` by `OtpCleanupService`.
- No provider secrets in the mobile apps. Flutter never generates or verifies OTPs locally.

## Localization

The new auth/OTP strings were added and **translated into all 9 supported languages**
(ar, de, en, es, fr, nl, pl, ro, uk): backend error messages in
`src/Taxi.Api/Resources/SharedResource.<lang>.json`, and customer-app UI strings in
`customertaxi/assets/l10n/<lang>.json`.
