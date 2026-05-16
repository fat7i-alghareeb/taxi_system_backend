# Stripe Setup — Backend (Taxi.Server)

Delete this file after setup is complete.

---

## 1. Create / log in to your Stripe account

Go to [dashboard.stripe.com](https://dashboard.stripe.com) and sign in or create an account.

---

## 2. Switch to Test mode

In the top-right corner of the Stripe dashboard there is a toggle:
**"Test mode"** / **"Live mode"**

Make sure **Test mode is ON** during development. All keys, webhooks, and payments in test mode are completely isolated from real money.

- Test mode keys start with `pk_test_` and `sk_test_`
- Live mode keys start with `pk_live_` and `sk_live_`

---

## 3. Get your API keys

Go to **Developers → API keys** (left sidebar).

You will see two keys:

| Key | Starts with | Goes in |
|-----|-------------|---------|
| Publishable key | `pk_test_...` | `Stripe:PublishableKey` in backend + returned to Flutter via `/api/config` |
| Secret key | `sk_test_...` | `Stripe:SecretKey` in backend only — never expose this |

Copy both. You will need them in step 5.

---

## 4. Create a webhook endpoint

Webhooks are how Stripe tells your backend "the payment succeeded / failed / was refunded".

### For production (deployed server)

1. Go to **Developers → Webhooks** in the Stripe dashboard.
2. Click **"Add endpoint"**.
3. Set the endpoint URL to:
   ```
   https://YOUR_DOMAIN/api/webhooks/stripe
   ```
4. Under **"Select events to listen to"**, add exactly these 4 events:
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
   - `payment_intent.canceled`
   - `charge.refunded`
5. Click **"Add endpoint"**.
6. On the endpoint detail page, click **"Reveal"** under **Signing secret**.
   Copy the value — it starts with `whsec_...`. This is your `Stripe:WebhookSecret`.

### For local development (your machine)

Your local server cannot receive webhooks from Stripe directly. Use the **Stripe CLI** instead.

**Install the Stripe CLI:**

- Windows: `winget install Stripe.StripeCLI` or download from [stripe.com/docs/stripe-cli](https://stripe.com/docs/stripe-cli)
- Mac: `brew install stripe/stripe-cli/stripe`

**Log in:**

```bash
stripe login
```

This opens a browser to authorize the CLI with your Stripe account.

**Forward webhooks to your local server:**

```bash
stripe listen --forward-to https://localhost:5001/api/webhooks/stripe
```

Replace `5001` with your actual API port (check `src/Taxi.Api/Properties/launchSettings.json`).

When it starts, it prints a **webhook signing secret** that looks like `whsec_...`. Copy it — this is your local `Stripe:WebhookSecret`. It is different from the production one.

**Leave this terminal running** while you test. Every payment event Stripe generates will be forwarded to your local endpoint.

---

## 5. Add keys to the project

Use .NET User Secrets so keys are never committed to git. Run these from the `TAXI_SERVER` root:

```bash
cd src/Taxi.Api

dotnet user-secrets set "Stripe:SecretKey"      "sk_test_YOUR_SECRET_KEY_HERE"
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_PUBLISHABLE_KEY_HERE"
dotnet user-secrets set "Stripe:WebhookSecret"  "whsec_YOUR_WEBHOOK_SECRET_HERE"
dotnet user-secrets set "Stripe:TestMode"        "true"
dotnet user-secrets set "FeatureFlags:StripeEnabled"  "true"
dotnet user-secrets set "FeatureFlags:SignalREnabled" "true"
```

**What each key does:**

| Key | Purpose |
|-----|---------|
| `Stripe:SecretKey` | Used server-side by `StripePaymentService` to create PaymentIntents |
| `Stripe:PublishableKey` | Returned to the Flutter app via `GET /api/config` so it can initialize the Payment Sheet |
| `Stripe:WebhookSecret` | Used by `StripeWebhookValidator` to verify that webhook requests really came from Stripe |
| `Stripe:TestMode` | Informational flag; set to `false` for production |
| `FeatureFlags:StripeEnabled` | When `false`, `RequestTripCommandHandler` skips payment entirely (cash flow) |
| `FeatureFlags:SignalREnabled` | When `false`, the Flutter app falls back to polling-only; SignalR coordinator is a no-op |

---

## 6. Verify the `/api/config` response

After running the API, hit `GET /api/config`. The response should include:

```json
{
  "stripeEnabled": true,
  "stripePublishableKey": "pk_test_...",
  "signalREnabled": true
}
```

The Flutter app reads this on startup and initializes `Stripe.publishableKey` automatically. If `stripeEnabled` is `false` or the key is empty, the Flutter Payment Sheet is never shown.

---

## 7. Test the payment flow

Stripe provides test card numbers that simulate real payments without any real money:

| Scenario | Card number | Expiry | CVC |
|----------|------------|--------|-----|
| Payment succeeds | `4242 4242 4242 4242` | Any future date | Any 3 digits |
| Payment requires 3D Secure auth | `4000 0025 0000 3155` | Any future date | Any 3 digits |
| Payment is declined | `4000 0000 0000 9995` | Any future date | Any 3 digits |
| Insufficient funds | `4000 0000 0000 0069` | Any future date | Any 3 digits |

For the billing address, use any values (country: `NL` matches the app's default, but anything works in test mode).

---

## 8. Trigger test webhooks manually (optional)

While the Stripe CLI is listening, you can trigger test events without going through the full booking flow:

```bash
# Simulate a successful payment
stripe trigger payment_intent.succeeded

# Simulate a failed payment
stripe trigger payment_intent.payment_failed

# Simulate a refund
stripe trigger charge.refunded
```

These fire the webhook at your local endpoint so you can verify the handlers in `HandleStripeWebhookCommandHandler`.

---

## 9. Switch to live mode for production

When ready to go live:

1. Turn off **Test mode** in the Stripe dashboard.
2. Get the **live** API keys (same **Developers → API keys** page, but now in Live mode).
3. Create a new webhook endpoint — the URL is still
   `https://api.fat7i.dev/api/webhooks/stripe`, only the signing
   secret changes (see step 10 below for the exact procedure).
4. Replace user secrets with live keys:
   ```bash
   dotnet user-secrets set "Stripe:SecretKey"      "sk_live_..."
   dotnet user-secrets set "Stripe:PublishableKey" "pk_live_..."
   dotnet user-secrets set "Stripe:WebhookSecret"  "whsec_..."
   dotnet user-secrets set "Stripe:TestMode"        "false"
   ```
5. In production, store secrets in environment variables or a secrets manager — not in `appsettings.json` or user secrets.

---

## 10. Configure the production webhook (api.fat7i.dev)

The production deployment fronts the API with Caddy at
`https://api.fat7i.dev`. The Stripe webhook controller is mounted
at `/api/webhooks/stripe`, so the full public URL is:

```text
https://api.fat7i.dev/api/webhooks/stripe
```

### Steps in the Stripe dashboard

1. Make sure the dashboard toggle in the top-right is set to the mode
   you want to configure — **Test mode** for staging-on-prod-domain, or
   **Live mode** for real money. You will end up creating **one webhook
   endpoint per mode**, each with its own signing secret.

2. Go to **Developers → Webhooks → Add endpoint**.

3. **Endpoint URL** — paste exactly:

   ```text
   https://api.fat7i.dev/api/webhooks/stripe
   ```

4. **Events to send** — click *"Select events"* and add these four (the
   same set the backend's `HandleStripeWebhookCommandHandler` reacts to):
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
   - `payment_intent.canceled`
   - `charge.refunded`

5. Click **Add endpoint**.

6. On the endpoint detail page click **"Reveal"** under **Signing
   secret** and copy the value (starts with `whsec_...`).

### Wire the secret into the server

On the production host, set `STRIPE_WEBHOOK_SECRET` in the `.env` file
that `docker-compose.prod.yml` reads:

```bash
# /path/to/TAXI_SERVER/.env
STRIPE_WEBHOOK_SECRET=whsec_xxxxxxxxxxxxxxxxxxxxxxxx
```

Then restart the API container so the new value is picked up:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d taxi.api
```

### Verify end-to-end

1. In the Stripe dashboard webhook page, click **"Send test webhook"**
   and pick `payment_intent.succeeded`.
2. Watch the API logs:
   ```bash
   docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f taxi.api
   ```
3. You should see the webhook handler log line and a `200` delivery in
   the Stripe dashboard's webhook attempts list. A `400` here usually
   means the signing secret in `.env` does not match the one shown in
   the Stripe dashboard.

### Switching from test to live mode

When you flip the Stripe dashboard from Test to Live, the webhook
endpoint URL stays the same (`https://api.fat7i.dev/api/webhooks/stripe`)
but the signing secret is different — repeat steps 2-6 above in **Live
mode**, then replace `STRIPE_WEBHOOK_SECRET` in `.env` with the live
secret and restart `taxi.api`.
