# PostgreSQL migration — Phase 4 runbook

Operational guide for SQLite → PostgreSQL data migration, schema alignment, and validation. This runbook targets **tooling and Docker only**; it does not replace EF Core migrations managed in `src/AiEmployee.Infrastructure`.

---

## 1. Architecture and constraints

- **Clean Architecture:** Domain, Application, Infrastructure, and API **C#** sources are unchanged by Phase 4 tooling. The **DataMigrator** (`tools/AiEmployee.DataMigrator`) references existing types only to read SQLite and write PostgreSQL.
- **Migrations:** Apply Postgres migrations via `AiEmployeePostgresDbContext` (API startup or `dotnet ef`). Do not edit generated migration files as part of routine ETL.
- **DateTime handling:** SQLite commonly yields `DateTimeKind.Unspecified`. Npgsql requires UTC for `timestamp with time zone`. Normalization runs **only inside** `SqliteToPostgresMigrator` before `SaveChangesAsync`.

---

## 2. PostgreSQL connectivity strategy (standard)

### Option A — Preferred: run tools on the Compose `backend` network

**Why:** The default `docker-compose.yml` does **not** publish Postgres port `5432` to the host. Services on the `backend` network reach Postgres at hostname **`postgres`**, port **5432**. This avoids exposing the database on localhost and matches how the **api** service connects.

**Requirements:**

- Postgres service running and healthy: `docker compose up -d postgres`
- Docker network name is typically **`{project}_backend`** (e.g. `aiemployee_backend`). Confirm with:

  ```bash
  docker network ls | findstr backend
  ```

**DataMigrator / EF pattern:** run the SDK container attached to that network, repo mounted at `/src`:

```bash
docker run --rm -v "%CD%:/src" -w /src --network aiemployee_backend mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet run --project tools/AiEmployee.DataMigrator -- \
  --source "Data Source=/src/migration-sqlite-snapshot.db" \
  --target "Host=postgres;Port=5432;Database=aiemployee;Username=aiemployee;Password=postgres" \
  --dry-run
```

(PowerShell: replace `%CD%` with the absolute repo path, e.g. `c:\Users\Public\Myapps\AiEmployee`.)

**EF Core from Docker (optional):** same image and network; set `Database__Provider=Npgsql` and `ConnectionStrings__DefaultConnection` to the same `Host=postgres;...` string, then run `dotnet ef database update` with `--project` / `--startup-project` / `--context AiEmployeePostgresDbContext`.

### Option B — Host-based tools: publish Postgres

Use when you must run `dotnet ef` or the migrator **on the host** with `Host=localhost`.

1. Add a **gitignored** override (do not commit secrets), e.g. `docker-compose.override.yml`:

   ```yaml
   services:
     postgres:
       ports:
         - "5432:5432"
   ```

2. Recreate Postgres: `docker compose up -d postgres`

3. Use a host connection string, e.g.  
   `Host=localhost;Port=5432;Database=aiemployee;Username=aiemployee;Password=postgres`

**Trade-off:** Postgres is reachable from the machine (and potentially the LAN, depending on firewall). Prefer Option A for CI and shared environments.

---

## 3. SQLite source file for ETL

- **Inside the API container**, SQLite often lives at `Data Source=/app/data/aiemployee.db`.
- **On the dev host**, copy a snapshot for reproducible runs (example):

  ```bash
  docker cp aiemployee-api-1:/app/data/aiemployee.db ./migration-sqlite-snapshot.db
  ```

- Point `--source` at that file. In Docker SDK runs, use  
  `Data Source=/src/migration-sqlite-snapshot.db` when the repo is mounted at `/src`.

---

## 4. Reproducible ETL and validation commands

Run from repo root unless noted. Adjust `--network` and volume path for your OS.

### 4.1 Dry run (no writes to Postgres)

```bash
docker run --rm -v "/path/to/AiEmployee:/src" -w /src --network aiemployee_backend mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet run --project tools/AiEmployee.DataMigrator -- \
  --source "Data Source=/src/migration-sqlite-snapshot.db" \
  --target "Host=postgres;Port=5432;Database=aiemployee;Username=aiemployee;Password=postgres" \
  --dry-run
```

**Expect:** `[DryRun]` batch logs per table, then `Dry run complete; skipping post-migration validation.`, exit code **0**.

### 4.2 Full migration (truncate application tables, then copy)

**CLI ordering:** `Microsoft.Extensions.Configuration.CommandLine` may treat the token after `--truncate` as its value. Always pass **`--batch-size` before `--truncate`** when both are used:

```bash
docker run --rm -v "/path/to/AiEmployee:/src" -w /src --network aiemployee_backend mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet run --project tools/AiEmployee.DataMigrator -- \
  --source "Data Source=/src/migration-sqlite-snapshot.db" \
  --target "Host=postgres;Port=5432;Database=aiemployee;Username=aiemployee;Password=postgres" \
  --batch-size 500 --truncate
```

**Expect:** Per-table insert logs, built-in post-migration validation, final line **`Validation result: PASSED`**, exit code **0**.

### 4.3 Validation only (no copy)

```bash
docker run --rm -v "/path/to/AiEmployee:/src" -w /src --network aiemployee_backend mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet run --project tools/AiEmployee.DataMigrator -- \
  --source "Data Source=/src/migration-sqlite-snapshot.db" \
  --target "Host=postgres;Port=5432;Database=aiemployee;Username=aiemployee;Password=postgres" \
  --validate-only
```

**Expect:** Row count lines `Source=N Target=N OK`, FK checks `orphan rows=0 OK`, **`Validation result: PASSED`**, exit code **0**.

---

## 5. API with PostgreSQL (Docker Compose)

```bash
docker compose up -d postgres
docker compose up -d --build api
docker compose logs api --tail 100
```

Compose defaults (see `docker-compose.yml`):

- `Database__Provider` → Npgsql (unless overridden in `.env`)
- `ConnectionStrings__DefaultConnection` → `Host=postgres;Port=5432;...`

**Smoke test (admin):**

```http
GET http://localhost:5155/admin/behaviors
X-Admin-Key: <value of Admin__ApiKey / ADMIN_API_KEY>
```

**Expect:** HTTP **200**. This API does not expose `/swagger` by default; use admin routes for smoke checks.

---

## 6. Docker image build (API)

The solution includes `tools/AiEmployee.DataMigrator`. The API **Dockerfile** must copy that project’s `.csproj` **before** `dotnet restore` so restore does not fail with **MSB3202** (missing project file):

```dockerfile
COPY tools/AiEmployee.DataMigrator/*.csproj    tools/AiEmployee.DataMigrator/
RUN dotnet restore
```

Only `.csproj` files are copied at that stage; full sources are copied with `COPY . .` afterward. Keeping `.dockerignore` tight avoids bloating the build context.

---

## 7. Troubleshooting

| Symptom | Likely cause | Mitigation |
|--------|----------------|------------|
| Cannot connect to Postgres from host | Port not published | Use Option A (Docker network) or Option B (ports) |
| `MSB3202` on `docker compose build api` | Migrator `.csproj` not copied before restore | Confirm Dockerfile `COPY tools/AiEmployee.DataMigrator/*.csproj` line |
| `Failed to convert '--batch-size' ... to Boolean` at `truncate` | Flag order | Use `--batch-size 500 --truncate` (not `--truncate --batch-size 500`) |
| `DateTimeKind.Unspecified` / timestamptz | SQLite → Npgsql | Ensure DataMigrator includes batch temporal normalization (tooling only) |
| API crash: SQLite + `Host=` in connection string | Stale image or `Database__Provider=Sqlite` with Postgres CS | Rebuild API image; align Provider and connection string |
| `libgssapi_krb5.so.2` in API logs | Npgsql / Kerberos on slim runtime | Usually benign if connections succeed; add distro package only if required |

---

## 8. Known limitations

- **Semantic time zones:** `DateTime.SpecifyKind(..., Utc)` preserves the clock reading and marks it UTC; it does not reinterpret local civil time. Confirm this matches product expectations for legacy SQLite data.
- **Swagger:** Not enabled by default; use documented HTTP endpoints for verification.
- **Secrets:** Use environment files or secret stores; do not commit passwords in compose files checked into git.

---

## 9. Related documentation

- [CONFIGURATION.md](./CONFIGURATION.md) — environment variables and providers  
- [DEPLOYMENT.md](./DEPLOYMENT.md) — broader deployment notes  
- [postgresql-cutover-phase5.md](./postgresql-cutover-phase5.md) — after data migration: Postgres-primary cutover and rollback  

---

## 10. Phase 4 completion checklist

- [ ] Postgres healthy in Compose  
- [ ] Postgres schema / migrations aligned with `AiEmployeePostgresDbContext`  
- [ ] Dry run completes with exit code 0  
- [ ] Full ETL completes; **`Validation result: PASSED`**  
- [ ] `docker compose up -d --build api` succeeds  
- [ ] API uses Npgsql against `postgres:5432`; admin smoke test returns 200  

When all items are satisfied, Phase 4 data migration is operationally complete.
