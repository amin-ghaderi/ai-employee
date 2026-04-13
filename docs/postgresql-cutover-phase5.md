# Phase 5 — PostgreSQL cutover (production orientation)

This document describes the **operational cutover** from SQLite to PostgreSQL for the AI Employee Platform after Phase 4 (data migration) is complete. It applies to **configuration and deployment only**; application C# and EF migrations are unchanged.

---

## 1. Final architecture overview

| Layer | Responsibility |
|--------|----------------|
| **PostgreSQL** | Primary relational store in Docker Compose and recommended production topology. |
| **API (`AiEmployee.Api`)** | Uses `Database__Provider` + `ConnectionStrings__DefaultConnection`. With **`Npgsql`**, both `AiEmployeePostgresDbContext` (migrations) and `AiEmployeeDbContext` target PostgreSQL per existing registration. |
| **SQLite** | **Deprecated for production.** Remains in the repository for development and **emergency rollback** only (no removal of code or packages in Phase 5). |
| **ai-service** | Unchanged; API reaches it via `Ai__BaseUrl`. |
| **admin-ui** | Consumes API at `VITE_API_URL`; no direct database access. |

```text
┌─────────────┐     HTTP      ┌─────────────┐
│  admin-ui   │ ────────────► │     API     │
└─────────────┘               └──────┬──────┘
                                     │ Npgsql
                                     ▼
                              ┌─────────────┐
                              │ PostgreSQL │
                              └─────────────┘
```

---

## 2. Deployment steps (Docker Compose)

1. **Ensure Postgres data and schema exist** (Phase 4: migrations + ETL). Postgres container must be **healthy**.
2. **Set secrets** in `.env` (gitignored), e.g. `POSTGRES_PASSWORD`, `ADMIN_API_KEY`. Do not commit real passwords.
3. **Default stack** (Postgres primary):

   ```bash
   docker compose up -d --build
   ```

4. **Full recycle** (e.g. after compose file changes):

   ```bash
   docker compose down
   docker compose up -d --build
   ```

5. **Verify** (see §4): `docker compose ps`, `docker compose logs api`, admin HTTP smoke test.

For ETL and host-vs-container connectivity patterns, see [postgresql-migration-phase4.md](./postgresql-migration-phase4.md).

---

## 3. Environment variables

### 3.1 API — PostgreSQL primary (Compose defaults)

| Variable | Example / default in `docker-compose.yml` |
|----------|----------------------------------------|
| `Database__Provider` | `Npgsql` (default via `${Database__Provider:-Npgsql}`) |
| `ConnectionStrings__DefaultConnection` | `Host=postgres;Port=5432;Database=aiemployee;Username=aiemployee;Password=${POSTGRES_PASSWORD:-postgres}` |
| `Ai__BaseUrl` | `http://ai-service:8000` |
| `Admin__ApiKey` | `${ADMIN_API_KEY:-your-secret-key}` (replace in real deployments) |

### 3.2 Rollback — SQLite (emergency only)

Use a **gitignored** `docker-compose.override.yml` or temporary env:

| Variable | Value |
|----------|--------|
| `Database__Provider` | `Sqlite` |
| `ConnectionStrings__DefaultConnection` | `Data Source=/app/data/aiemployee.db` |

Ensure a valid `aiemployee.db` exists under the **`api-data`** volume mount (`/app/data`) before switching back.

### 3.3 Production (Kubernetes / VM)

- Set **`Database__Provider=Npgsql`** (or `PostgreSQL` if your config layer accepts it).
- Supply a managed connection string (host, user, password from secret store).
- **Omit** SQLite file mounts unless rollback is required.
- See [examples/appsettings.Production.example.json](./examples/appsettings.Production.example.json) for JSON-oriented templates.

---

## 4. Verification procedures

### 4.1 PostgreSQL readiness

```bash
docker compose ps
docker compose logs postgres --tail 30
```

**Expect:** `postgres` service **healthy**; logs show *ready to accept connections*.

**Schema / migrations history:**

```bash
docker exec aiemployee-postgres psql -U aiemployee -d aiemployee -c \
  "SELECT tablename FROM pg_tables WHERE schemaname='public' AND tablename ILIKE '%migration%';"
```

**Expect:** `__EFMigrationsHistory_Postgres` exists.

### 4.2 API runtime (Npgsql, no SQLite path active)

```bash
docker compose logs api --tail 150
```

**Expect:** EF initialized with **Npgsql**; connections to **`postgres:5432`** (or your managed host). When `Database__Provider` is **not** `Sqlite`, you should **not** see SQLite provider initialization for `AiEmployeeDbContext`.

### 4.3 Admin API smoke test

```http
GET http://localhost:5155/admin/behaviors
X-Admin-Key: <your Admin__ApiKey>
```

**Expect:** **HTTP 200**.

### 4.4 Optional manual checks

| Area | Suggestion |
|------|------------|
| **Admin UI** | Open `http://localhost:5173`; confirm bots/settings load. |
| **Bot configuration** | `GET /admin/bots` with `X-Admin-Key`. |
| **AI service** | Confirm API logs show successful outbound calls to `Ai__BaseUrl` when exercising a feature that uses the judge/LLM path. |
| **Telegram** | Send a test message to a configured bot; confirm webhook delivery (requires `App__PublicBaseUrl` and integration setup per [TELEGRAM_WEBHOOKS.md](./TELEGRAM_WEBHOOKS.md)). |

---

## 5. SQLite decommissioning (configuration level)

- **Production examples** use PostgreSQL only; SQLite connection strings are removed from **production-oriented** samples (see `docs/examples/appsettings.Production.example.json`).
- **Repository:** SQLite EF provider, `appsettings.json` defaults for local dev, and **api-data** volume in Compose remain for **rollback and developer workflows** — no binary or migration removal in Phase 5.
- **Compose `api-data` volume:** Not required for Postgres-only **runtime** (API does not read `/app/data` when provider is `Npgsql`). The volume is **retained in `docker-compose.yml`** so rollback to SQLite does not require volume recreation.

---

## 6. Rollback instructions (target: minutes)

**Precondition:** Last known good `aiemployee.db` available in `api-data` (or restore file into `/app/data`).

1. Create or edit **`docker-compose.override.yml`** (gitignored):

   ```yaml
   services:
     api:
       environment:
         - Database__Provider=Sqlite
         - ConnectionStrings__DefaultConnection=Data Source=/app/data/aiemployee.db
   ```

2. **Redeploy:**

   ```bash
   docker compose up -d api
   ```

3. **Validate:** `docker compose logs api` shows SQLite provider for `AiEmployeeDbContext`; run admin smoke tests.

4. **Forward path:** Remove the override (or set `Database__Provider` back to `Npgsql` and Postgres connection string), then `docker compose up -d api` again.

**Typical time:** compose recreate + API startup (order of **1–3 minutes**), excluding time to obtain a good SQLite file if missing.

---

## 7. Known issues and mitigations

| Issue | Mitigation |
|--------|------------|
| API crashes with “`host` is not supported” on SQLite builder | `Database__Provider` and connection string **misaligned** — ensure both point to the same engine (see §3). |
| Stale image after compose changes | `docker compose build api --no-cache` then `up -d`. |
| Kerberos / `libgssapi` noise in logs | Usually benign if DB connectivity succeeds; see Phase 4 runbook troubleshooting. |
| Telegram webhooks fail after cutover | Re-check `App__PublicBaseUrl` and **Sync Webhook** for integrations; unrelated to DB engine but often validated in the same window. |

---

## 8. Related documentation

- [postgresql-migration-phase4.md](./postgresql-migration-phase4.md) — Data migration, Docker networking, ETL commands.  
- [DEPLOYMENT.md](./DEPLOYMENT.md) — Broader deployment and persistence notes.  
- [CONFIGURATION.md](./CONFIGURATION.md) — Environment variable reference.  

---

## 9. Phase 5 completion checklist

- [ ] Postgres **healthy**; `__EFMigrationsHistory_Postgres` present.  
- [ ] API defaults to **`Npgsql`** in Compose (or explicit prod env).  
- [ ] `docker compose up -d --build` succeeds; API logs show **Npgsql** to Postgres.  
- [ ] `GET /admin/behaviors` returns **200** with valid admin key.  
- [ ] Rollback steps documented and tested on a non-prod environment at least once.  
- [ ] Production examples show **PostgreSQL only**; SQLite marked deprecated for prod.  

When satisfied, declare: **PostgreSQL is the primary production database** for deployments following this runbook.
