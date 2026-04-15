# Deployment guide

## 1. Development (ngrok)

**Goal:** Expose the local API on **HTTPS** so Telegram can call your webhooks.

### Option A — Docker Compose ngrok service

The repo includes an **`ngrok`** service that forwards **HTTPS** traffic to the **`api`** container (`api:5155`). It starts with the default stack (`docker compose up -d`).

```bash
# Copy .env.example to .env and set NGROK_AUTHTOKEN (https://dashboard.ngrok.com/get-started/your-authtoken)
docker compose up -d
```

1. Open the ngrok dashboard (e.g. `http://localhost:4040`) and copy the **HTTPS** forwarding URL.
2. Set **`App__PublicBaseUrl`** on the **`api`** service to that URL (no trailing slash). Restart **`api`** if it was already running.
3. Call **Sync Webhook** for each Telegram integration (Admin UI or API).

### Option B — ngrok CLI against local `dotnet run`

```bash
ngrok http 5155
```

Set **`App:PublicBaseUrl`** in user secrets or `appsettings.Development.json` to the https ngrok URL.

### Option C — HTTP only (local smoke tests)

Telegram’s **production** servers require HTTPS. For **local adapter tests** without Telegram, HTTP is fine. **`PublicBaseUrlProvider`** allows HTTP when not in Production.

---

## 2. Production (custom domain)

1. **TLS termination** — Terminate HTTPS at your reverse proxy (nginx, Traefik, cloud LB) or Kestrel with a real certificate.
2. **`ASPNETCORE_ENVIRONMENT=Production`**
3. Set **`App__PublicBaseUrl=https://api.yourdomain.com`** (must be **HTTPS**).
4. Set **`Admin__ApiKey`** to a strong random value; configure admin-ui **`VITE_ADMIN_KEY`** to match if used.
5. Set **`Ai__BaseUrl`** to the reachable Python service URL (internal cluster DNS or public URL per your topology).
6. Run **EF migrations** before or on startup per your policy.
7. For each Telegram bot: create **`BotIntegration`** (`telegram`, token in **`ExternalId`**), then **Sync Webhook**.

**DNS:** Point `api.yourdomain.com` to your load balancer / ingress.

---

## 3. Docker deployment

Build and run the stack:

```bash
docker compose build
docker compose up -d
```

Services:

| Service | Port (default) | Role |
|---------|----------------|------|
| **api** | 5155 | ASP.NET Core API |
| **ai-service** | 8000 | Python LLM gateway |
| **admin-ui** | 5173 | Vite dev-style UI (bind-mount) |
| **ollama** | 11434 | Local inference (optional) |
| **ngrok** | 4040 | Tunnel UI + HTTPS forwarding to **api** |

**Persistence:** The API requires **PostgreSQL with pgvector** (see **`postgres`** in [`docker-compose.yml`](../docker-compose.yml)). See **[PHASE7_POSTGRES_ONLY.md](./PHASE7_POSTGRES_ONLY.md)**, **[postgresql-migration-phase4.md](./postgresql-migration-phase4.md)** (archived ETL notes), and **[postgresql-cutover-phase5.md](./postgresql-cutover-phase5.md)**.

**Secrets:** Use **`docker-compose.override.yml`** (gitignored) or orchestrator secrets for **`Admin__ApiKey`**, **`Telegram__BotToken`** (if used), and ngrok token.

See [`docker-compose.yml`](../docker-compose.yml) and example snippets under [`docs/examples/`](./examples/).

---

## 4. Health and operations

- Confirm **`GET /admin/integrations/{id}/webhook-status`** returns **`active`** (or investigate **`mismatch`** / **`not_registered`**).
- Watch API logs for **`Telegram webhook:`** and **`telegram_`** prefixes.
- After rotating tokens, update **`BotIntegrations.ExternalId`** and **Sync Webhook** again.

---

## 5. Related documentation

- [TELEGRAM_WEBHOOKS.md](./TELEGRAM_WEBHOOKS.md) — Architecture, admin API, security.
- [CONFIGURATION.md](./CONFIGURATION.md) — Environment variables.
- [postgresql-migration-phase4.md](./postgresql-migration-phase4.md) — Phase 4 Postgres migration runbook (ETL, Docker networking, validation).
- [postgresql-cutover-phase5.md](./postgresql-cutover-phase5.md) — Phase 5 Postgres-primary cutover, verification, rollback.
- [OPERATIONAL_CHECKLIST.md](./OPERATIONAL_CHECKLIST.md) — Go-live checklist.
