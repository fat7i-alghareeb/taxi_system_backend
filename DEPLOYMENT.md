# Taxi Server Production Deployment

This guide deploys the API on an Ubuntu VPS with Docker Compose, Postgres, Caddy HTTPS, Seq, Prometheus, and Grafana.

## 1. What You Need

- Ubuntu VPS IP address.
- A domain or subdomain, for example `api.example.com`.
- Access to this GitHub repository settings.
- Firebase service account JSON.
- Google Maps API key.
- Stripe keys and webhook secret if Stripe is enabled.

## 2. Prepare DNS

In your domain DNS panel, create:

```text
Type: A
Name: api
Value: YOUR_VPS_IPV4
TTL: Auto or 300
```

Only add an `AAAA` record if the VPS has working IPv6. Before starting Caddy, check:

```bash
dig +short api.example.com
```

It must return your VPS IP.

## 3. First SSH Into The VPS With Your Main User

Use the main VPS user your provider gave you. It may be `root`, `ubuntu`, `admin`, or another sudo-capable user. In this guide, replace `YOUR_VPS_USER` with that user.

From your machine:

```bash
ssh YOUR_VPS_USER@YOUR_VPS_IP
```

Check which user you are logged in as:

```bash
whoami
```

If the command prints `root`, that is okay. If it prints another user, make sure sudo works:

```bash
sudo whoami
```

It should print `root`.

Do not create another Linux user for this guide. The GitHub key below is still repo-only, so it only gives this VPS read access to this one repository.

## 4. Install Docker And Tools

Run on the VPS as your main user:

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl git ufw dnsutils nano openssl

for pkg in docker.io docker-compose docker-compose-v2 docker-doc podman-docker containerd runc; do
  sudo apt-get remove -y "$pkg" || true
done

sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc

sudo tee /etc/apt/sources.list.d/docker.sources > /dev/null <<EOF
Types: deb
URIs: https://download.docker.com/linux/ubuntu
Suites: $(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}")
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
EOF

sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo systemctl enable --now docker
sudo usermod -aG docker "$USER"
```

Log out and back in so the `docker` group applies:

```bash
exit
ssh YOUR_VPS_USER@YOUR_VPS_IP
docker compose version
docker run --rm hello-world
```

## 5. Firewall

Allow SSH, HTTP, and HTTPS:

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
sudo ufw status
```

Do not open Postgres, Seq, Prometheus, Grafana, or API port `5001` publicly. Production Compose binds the API and dashboards to `127.0.0.1` only.

## 6. Create A Repo-Only GitHub SSH Key

On the VPS as your main user:

```bash
ssh-keygen -t ed25519 -C "taxi-server-vps-deploy-key" -f ~/.ssh/taxi_server_deploy -N ""

cat > ~/.ssh/config <<EOF
Host github-taxi-server
  HostName github.com
  User git
  IdentityFile ~/.ssh/taxi_server_deploy
  IdentitiesOnly yes
EOF

chmod 600 ~/.ssh/config
cat ~/.ssh/taxi_server_deploy.pub
```

Copy the printed public key.

In GitHub:

1. Open the repository.
2. Go to `Settings` -> `Deploy keys`.
3. Click `Add deploy key`.
4. Title: `taxi-server-vps`.
5. Paste the public key.
6. Leave `Allow write access` unchecked.
7. Click `Add key`.

Back on the VPS, test:

```bash
ssh -T github-taxi-server
```

GitHub should recognize the key. It may say shell access is not provided; that is normal.

## 7. Clone The Project

Replace `OWNER` and `REPO`:

```bash
sudo mkdir -p /srv/taxi-server
sudo chown "$USER":"$USER" /srv/taxi-server
git clone git@github-taxi-server:OWNER/REPO.git /srv/taxi-server
cd /srv/taxi-server
```

## 8. Create The Production `.env`

```bash
cp .env.example .env
chmod 600 .env
nano .env
```

Set at least these values:

```dotenv
DOMAIN=api.example.com
CADDY_EMAIL=you@example.com
API_BASE_URL=https://api.example.com
APP_ALLOWED_ORIGIN_0=https://api.example.com

POSTGRES_USER=taxi_prod
POSTGRES_PASSWORD=use_a_long_random_password
JWT_SECRET=use_a_long_random_secret
GOOGLE_MAPS_API_KEY=your_google_maps_key
FIREBASE_CREDENTIALS='{"type":"service_account", "...":"..."}'

STRIPE_SECRET_KEY=sk_live_or_test_...
STRIPE_PUBLISHABLE_KEY=pk_live_or_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
STRIPE_TEST_MODE=false
STRIPE_ENABLED=true

INVOICE_ISSUER_NAME=Fat7i
INVOICE_ISSUER_ADDRESS=Your company address
INVOICE_ISSUER_VAT_NUMBER=
GRAFANA_ADMIN_PASSWORD=use_a_long_random_password
FORWARDED_HEADERS_KNOWN_NETWORK_0=172.16.0.0/12
```

Generate secrets on the VPS:

```bash
openssl rand -base64 48
```

For Firebase, use a single-line JSON value. If you upload the Firebase JSON file to the VPS temporarily:

```bash
python3 -c 'import json; print(json.dumps(json.load(open("firebase-service-account.json"))))'
```

Paste the output into `FIREBASE_CREDENTIALS='...'`, then delete the temporary JSON file:

```bash
rm firebase-service-account.json
```

## 9. Stripe Webhook

In Stripe Dashboard, create a webhook endpoint:

```text
https://api.example.com/api/webhooks/stripe
```

Subscribe to:

```text
payment_intent.succeeded
payment_intent.payment_failed
payment_intent.canceled
charge.refunded
```

Copy the signing secret into `STRIPE_WEBHOOK_SECRET`.

## 10. Start Production

From `/srv/taxi-server`:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml config
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

Watch logs:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f taxi.api
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f caddy
```

The API automatically applies EF migrations and seeds admin users on startup.

## 11. Check It Works

From your local machine:

```bash
curl -I https://api.example.com
```

Expected result can be `404`, `401`, or another API response, but TLS must work and the server must answer.

Test admin login:

```bash
curl -X POST https://api.example.com/api/v1/auth/admin/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"admin"}'
```

Seeded production admin users are:

```text
admin / admin
admin2 / admin2
```

They are marked as requiring password reset. Change them immediately after first login.

## 12. Private Dashboards By SSH Tunnel

From your local machine:

```bash
ssh -L 3000:localhost:3000 -L 9090:localhost:9090 -L 8081:localhost:8081 YOUR_VPS_USER@YOUR_VPS_IP
```

Then open:

```text
Grafana:    http://localhost:3000
Prometheus: http://localhost:9090
Seq:        http://localhost:8081
```

## 13. Update The App Later

```bash
cd /srv/taxi-server
git pull --ff-only
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f taxi.api
```

## 14. Back Up The Database

```bash
cd /srv/taxi-server
mkdir -p backups
docker compose -f docker-compose.yml -f docker-compose.prod.yml exec -T postgres \
  sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB"' \
  > "backups/taxi_$(date +%F_%H%M%S).sql"
```

Copy backups off the VPS regularly.

## 15. Useful Commands

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs --tail=200 taxi.api
docker compose -f docker-compose.yml -f docker-compose.prod.yml restart taxi.api
docker compose -f docker-compose.yml -f docker-compose.prod.yml down
```

Do not use `down -v` in production unless you intentionally want to delete named volumes, including the database.

## 16. Files This Deployment Uses

- `docker-compose.yml`: base services.
- `docker-compose.prod.yml`: production ports, secrets, Caddy, persistent upload volume.
- `Caddyfile.prod`: HTTPS reverse proxy.
- `.env`: real production secrets, not committed.
- `.env.example`: template only.
