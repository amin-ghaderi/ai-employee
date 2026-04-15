# Phase 5 — PostgreSQL cutover (production orientation)

> **Phase 7:** SQLite and `Database__Provider` are **removed**. The API always uses PostgreSQL via **`AiEmployeeDbContext`** and **`ConnectionStrings__DefaultConnection`**. See [PHASE7_POSTGRES_ONLY.md](./PHASE7_POSTGRES_ONLY.md).

This document describes the **operational cutover** from SQLite to PostgreSQL for the AI Employee Platform after Phase 4 (data migration) is complete. It applies to **configuration and deployment only**; application C# and EF migrations are unchanged.

---

## 1. Final architecture overview

| Layer | Responsibility |
|--------|----------------|
| **PostgreSQL** | Primary relational store in Docker Compose and recommended production topology. |
| **API (`AiEmployee.Api`)** | Single **`AiEmployeeDbContext`** (Npgsql + pgvector). **`ConnectionStrings__DefaultConnection`** must point at PostgreSQL in every environment. |
| **SQLite** | **Removed** in Phase 7. Historical rollback notes in later sections are **obsolete**. |
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

### 3.1 API — PostgreSQL (Compose defaults)

| Variable | Example / default in `docker-compose.yml` |
|----------|----------------------------------------|
| `ConnectionStrings__DefaultConnection` | `Host=postgres;Port=5432;Database=aiemployee;Username=aiemployee;Password=${POSTGRES_PASSWORD:-postgres}` |
| `Ai__BaseUrl` | `http://ai-service:8000` |
| `Admin__ApiKey` | `${ADMIN_API_KEY:-your-secret-key}` (replace in real deployments) |

### 3.2 Production (Kubernetes / VM)

- Supply a managed **PostgreSQL** connection string (pgvector-capable).
- See [examples/appsettings.Production.example.json](./examples/appsettings.Production.example.json) and [PHASE7_POSTGRES_ONLY.md](./PHASE7_POSTGRES_ONLY.md).

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

### 4.2 API runtime (Npgsql)

```bash
docker compose logs api --tail 150
```

**Expect:** EF initialized with **Npgsql**; connections to **`postgres:5432`** (or your managed host). There is no alternate database provider.

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

## 5. SQLite decommissioning (Phase 7)

SQLite packages, migrations, and provider switching are **removed**. Local development and tests use **PostgreSQL** (local install or Docker; integration tests use Testcontainers). The **`api-data`** volume in Compose remains available for arbitrary file storage if needed; it is not used for a default database file.

---

## 6. Rollback via SQLite (obsolete)

**No longer supported** as of Phase 7. Restore from **PostgreSQL backups** or redeploy a previous container image if required.

---

## 7. Known issues and mitigations

| Issue | Mitigation |
|--------|------------|
| API fails to connect to Postgres | Verify **`ConnectionStrings__DefaultConnection`** (host, port, credentials) and that the Postgres service is healthy. |
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
- [ ] `docker compose up -d --build` succeeds; API logs show **Npgsql** to Postgres.  
- [ ] `GET /admin/behaviors` returns **200** with valid admin key.  
- [ ] Production examples show **PostgreSQL only** (see Phase 7 doc).  

When satisfied, declare: **PostgreSQL is the primary production database** for deployments following this runbook.
